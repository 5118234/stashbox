﻿using Stashbox.Entity;
using Stashbox.Entity.Resolution;
using Stashbox.Exceptions;
using Stashbox.Registration;
using Stashbox.Resolution;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Stashbox.BuildUp.Expressions
{
    internal class ConstructorSelector : IConstructorSelector
    {
        public ResolutionConstructor CreateResolutionConstructor(IContainerContext containerContext,
            IServiceRegistration serviceRegistration,
            ResolutionContext resolutionContext,
            ConstructorInformation constructor)
        {
            var paramLength = constructor.Parameters.Length;
            var parameterExpressions = new Expression[paramLength];

            for (var i = 0; i < paramLength; i++)
            {
                var parameter = constructor.Parameters[i];

                var expression = containerContext.ResolutionStrategy.BuildResolutionExpression(containerContext,
                    resolutionContext, parameter, serviceRegistration.RegistrationContext.InjectionParameters);

                parameterExpressions[i] = expression ?? throw new ResolutionFailedException(serviceRegistration.ImplementationType,
                    $"Constructor {constructor.Constructor}, unresolvable parameter: ({parameter.Type}){parameter.ParameterName}");
            }

            return new ResolutionConstructor { Constructor = constructor.Constructor, Parameters = parameterExpressions };
        }

        public ResolutionConstructor SelectConstructor(IContainerContext containerContext,
            IServiceRegistration serviceRegistration,
            ResolutionContext resolutionContext,
            ConstructorInformation[] constructors)
        {
            var length = constructors.Length;
            var checkedConstructors = new Dictionary<ConstructorInfo, TypeInformation>();
            for (var i = 0; i < length; i++)
            {
                var constructor = constructors[i];

                if (!this.TryBuildResolutionConstructor(constructor, resolutionContext, containerContext,
                    serviceRegistration, out var failedParameter, out var parameterExpressions, true))
                {
                    checkedConstructors.Add(constructor.Constructor, failedParameter);
                    continue;
                }

                return new ResolutionConstructor { Constructor = constructor.Constructor, Parameters = parameterExpressions };
            }

            if (containerContext.ContainerConfigurator.ContainerConfiguration.UnknownTypeResolutionEnabled)
                for (var i = 0; i < length; i++)
                {
                    var constructor = constructors[i];
                    if (this.TryBuildResolutionConstructor(constructor, resolutionContext, containerContext,
                        serviceRegistration, out var failedParameter, out var parameterExpressions))
                        return new ResolutionConstructor { Constructor = constructor.Constructor, Parameters = parameterExpressions };
                }

            if (resolutionContext.NullResultAllowed)
                return null;

            var stringBuilder = new StringBuilder();
            foreach (var checkedConstructor in checkedConstructors)
                stringBuilder.AppendLine($"Checked constructor {checkedConstructor.Key}, unresolvable parameter: ({checkedConstructor.Value.Type}){checkedConstructor.Value.ParameterName}");

            throw new ResolutionFailedException(serviceRegistration.ImplementationType, stringBuilder.ToString());
        }

        private bool TryBuildResolutionConstructor(
            ConstructorInformation constructor,
            ResolutionContext resolutionContext,
            IContainerContext containerContext,
            IServiceRegistration serviceRegistration,
            out TypeInformation failedParameter,
            out Expression[] parameterExpressions,
            bool skipUknownResolution = false)
        {
            var paramLength = constructor.Parameters.Length;
            parameterExpressions = new Expression[paramLength];
            failedParameter = null;
            for (var i = 0; i < paramLength; i++)
            {
                var parameter = constructor.Parameters[i];

                parameterExpressions[i] = containerContext.ResolutionStrategy.BuildResolutionExpression(containerContext, resolutionContext,
                    parameter, serviceRegistration.RegistrationContext.InjectionParameters, skipUknownResolution);

                if (parameterExpressions[i] == null)
                {
                    failedParameter = parameter;
                    return false;
                }
            }

            return true;
        }
    }
}
