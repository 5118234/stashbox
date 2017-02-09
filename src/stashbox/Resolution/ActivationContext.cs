﻿using System;
using System.Linq.Expressions;
using Stashbox.BuildUp.Expressions;
using Stashbox.Entity;
using Stashbox.Exceptions;
using Stashbox.Infrastructure;
using Stashbox.Infrastructure.Resolution;

namespace Stashbox.Resolution
{
    internal class ActivationContext : IActivationContext
    {
        private readonly IContainerContext containerContext;
        private readonly IResolverSelector resolverSelector;

        public ActivationContext(IContainerContext containerContext, IResolverSelector resolverSelector)
        {
            this.containerContext = containerContext;
            this.resolverSelector = resolverSelector;
        }

        public object Activate(ResolutionInfo resolutionInfo, TypeInformation typeInfo)
        {
            var cachedFactory = this.containerContext.DelegateRepository.GetDelegateCacheOrDefault(typeInfo) ??
                this.containerContext.DelegateRepository.GetWrapperDelegateCacheOrDefault(typeInfo);

            return cachedFactory != null ? cachedFactory() : this.ActivateType(resolutionInfo, typeInfo);
        }

        public Delegate ActivateFactory(ResolutionInfo resolutionInfo, TypeInformation typeInfo, Type[] parameterTypes)
        {
            var cachedFactory = this.containerContext.DelegateRepository.GetFactoryDelegateCacheOrDefault(typeInfo, parameterTypes);
            return cachedFactory ?? ActivateFactoryDelegate(resolutionInfo, typeInfo, parameterTypes);
        }
        
        private object ActivateType(ResolutionInfo resolutionInfo, TypeInformation typeInfo)
        {
            var registration = this.containerContext.RegistrationRepository.GetRegistrationOrDefault(typeInfo);
            if (registration != null)
            {
                var ragistrationFactory = ExpressionDelegateFactory.CompileObjectExpression(registration.GetExpression(resolutionInfo, typeInfo));
                this.containerContext.DelegateRepository.AddServiceDelegate(typeInfo, ragistrationFactory);
                return ragistrationFactory();
            }

            Resolver resolver;
            if (!this.resolverSelector.TryChooseResolver(this.containerContext, typeInfo, out resolver))
                throw new ResolutionFailedException(typeInfo.Type.FullName);

            var factory = ExpressionDelegateFactory.CompileObjectExpression(resolver.GetExpression(resolutionInfo));
            this.containerContext.DelegateRepository.AddWrapperDelegate(new WrappedDelegateInformation
            {
                DependencyName = typeInfo.DependencyName,
                WrappedType = resolver.WrappedType,
                DelegateReturnType = typeInfo.Type
            }, factory);
            return factory();
        }

        private Delegate ActivateFactoryDelegate(ResolutionInfo resolutionInfo, TypeInformation typeInfo, Type[] parameterTypes)
        {
            Expression initExpression;
            var registration = this.containerContext.RegistrationRepository.GetRegistrationOrDefault(typeInfo);
            if (registration == null)
            {
                Resolver resolver;
                if (!this.resolverSelector.TryChooseResolver(this.containerContext, typeInfo, out resolver))
                    throw new ResolutionFailedException(typeInfo.Type.FullName);

                initExpression = resolver.GetExpression(resolutionInfo);
            }
            else
                initExpression = registration.GetExpression(resolutionInfo, typeInfo);
            
            var factory = Expression.Lambda(initExpression, resolutionInfo.ParameterExpressions).Compile();
            this.containerContext.DelegateRepository.AddFactoryDelegate(typeInfo, parameterTypes, factory);
            return factory;
        }
    }
}
