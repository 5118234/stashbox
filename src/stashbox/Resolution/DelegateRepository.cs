﻿using System;
using System.Linq;
using Stashbox.Entity;
using Stashbox.Infrastructure;
using Stashbox.Utils;

namespace Stashbox.Resolution
{
    internal class DelegateRepository : IDelegateRepository
    {
        private readonly HashMap<Type, Func<object>> serviceDelegates;
        private readonly HashMap<string, Func<object>> keyedServiceDelegates;
        private readonly RandomAccessArray<Type> typeMap;
        private readonly HashMap<Type, string, Func<object>> wrapperDelegates;
        private readonly HashMap<Type, string, Delegate> factoryDelegates;

        public DelegateRepository()
        {
            this.serviceDelegates = new HashMap<Type, Func<object>>();
            this.wrapperDelegates = new HashMap<Type, string, Func<object>>();
            this.keyedServiceDelegates = new HashMap<string, Func<object>>();
            this.factoryDelegates = new HashMap<Type, string, Delegate>();
            this.typeMap = new RandomAccessArray<Type>();
        }

        public Func<object> GetWrapperDelegateCacheOrDefault(TypeInformation typeInfo)
        {
            var wrappedType = typeMap.Load(typeInfo.Type.GetHashCode());
            if (wrappedType == null)
                return null;

            var key = this.GetKey(typeInfo.Type, typeInfo.DependencyName);
            return this.wrapperDelegates.GetOrDefault(wrappedType, key);
        }

        public Delegate GetFactoryDelegateCacheOrDefault(TypeInformation typeInfo, Type[] parameterTypes)
        {
            var key = this.GetFactoryKey(parameterTypes, typeInfo.DependencyName);
            return this.factoryDelegates.GetOrDefault(typeInfo.Type, key);
        }

        public Func<object> GetDelegateCacheOrDefault(TypeInformation typeInfo)
        {
            return typeInfo.DependencyName == null ? this.serviceDelegates.GetOrDefault(typeInfo.Type)
                : this.keyedServiceDelegates.GetOrDefault(this.GetKey(typeInfo.Type, typeInfo.DependencyName));
        }

        public void AddWrapperDelegate(WrappedDelegateInformation typeInfo, Func<object> factory)
        {
            var key = this.GetKey(typeInfo.DelegateReturnType, typeInfo.DependencyName);
            this.typeMap.Store(typeInfo.DelegateReturnType.GetHashCode(), typeInfo.WrappedType);
            this.wrapperDelegates.AddOrUpdate(typeInfo.WrappedType, key, factory);
        }

        public void AddFactoryDelegate(TypeInformation typeInfo, Type[] parameterTypes, Delegate factory)
        {
            var key = this.GetFactoryKey(parameterTypes, typeInfo.DependencyName);
            this.factoryDelegates.AddOrUpdate(typeInfo.Type, key, factory);
        }
        public void AddServiceDelegate(TypeInformation typeInfo, Func<object> factory)
        {
            if (typeInfo.DependencyName == null)
                this.serviceDelegates.AddOrUpdate(typeInfo.Type, factory);
            else
                this.keyedServiceDelegates.AddOrUpdate(this.GetKey(typeInfo.Type, typeInfo.DependencyName), factory);
        }

        public void InvalidateDelegateCache(TypeInformation typeInfo)
        {
            this.serviceDelegates.Clear(typeInfo.Type);
            this.wrapperDelegates.Clear(typeInfo.Type);
            this.factoryDelegates.Clear(typeInfo.Type);
            this.keyedServiceDelegates.Clear(this.GetKey(typeInfo.Type, typeInfo.DependencyName));
        }

        private string GetKey(Type type, string name) =>
            NameGenerator.GetRegistrationName(type, type, name);

        private string GetFactoryKey(Type[] types, string name) => types.Select(type => NameGenerator.GetRegistrationName(type, type))
            .Aggregate((current, next) => current + next) + name;
    }
}
