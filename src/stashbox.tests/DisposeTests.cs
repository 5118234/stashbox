﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stashbox.Attributes;
using Stashbox.Infrastructure;
using System;

namespace Stashbox.Tests
{
    [TestClass]
    public class DisposeTests
    {
        [TestMethod]
        public void DisposeTests_Singleton()
        {
            ITest1 test;
            ITest2 test2;
            Test3 test3;
            using (IStashboxContainer container = new StashboxContainer())
            {
                container.RegisterType<ITest2, Test2>();
                container.RegisterType<Test3>();
                container.RegisterSingleton<ITest1, Test1>();
                test = container.Resolve<ITest1>();
                test2 = container.Resolve<ITest2>();
                test3 = container.Resolve<Test3>();
            }

            Assert.IsTrue(test.Disposed);
            Assert.IsTrue(test2.Test1.Disposed);
            Assert.IsTrue(test3.Test1.Disposed);
        }

        [TestMethod]
        public void DisposeTests_Instance()
        {
            ITest1 test = new Test1();
            ITest2 test2;
            Test3 test3;
            using (IStashboxContainer container = new StashboxContainer())
            {
                container.RegisterType<ITest2, Test2>();
                container.RegisterType<Test3>();
                container.RegisterInstance<ITest1>(test);
                test2 = container.Resolve<ITest2>();
                test3 = container.Resolve<Test3>();
            }

            Assert.IsTrue(test.Disposed);
            Assert.IsTrue(test2.Test1.Disposed);
            Assert.IsTrue(test3.Test1.Disposed);
        }

        [TestMethod]
        public void DisposeTests_WireUp()
        {
            ITest1 test = new Test1();
            ITest2 test2;
            Test3 test3;
            using (IStashboxContainer container = new StashboxContainer())
            {
                container.RegisterType<ITest2, Test2>();
                container.RegisterType<Test3>();
                container.WireUp<ITest1>(test);
                test2 = container.Resolve<ITest2>();
                test3 = container.Resolve<Test3>();
            }

            Assert.IsTrue(test.Disposed);
            Assert.IsTrue(test2.Test1.Disposed);
            Assert.IsTrue(test3.Test1.Disposed);
        }

        [TestMethod]
        public void DisposeTests_TrackTransientDisposal()
        {
            ITest1 test;
            ITest2 test2;
            Test3 test3;
            using (IStashboxContainer container = new StashboxContainer(true))
            {
                container.RegisterType<ITest2, Test2>();
                container.RegisterType<Test3>();
                container.RegisterType<ITest1, Test1>();
                test = container.Resolve<ITest1>();
                test2 = container.Resolve<ITest2>();
                test3 = container.Resolve<Test3>();
            }

            Assert.IsTrue(test.Disposed);
            Assert.IsTrue(test2.Test1.Disposed);
            Assert.IsTrue(test3.Test1.Disposed);
        }

        public interface ITest1 : IDisposable { bool Disposed { get; } }

        public interface ITest2 { ITest1 Test1 { get; } }

        public class Test1 : ITest1
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                this.Disposed = true;
            }
        }

        public class Test2 : ITest2
        {
            public ITest1 Test1 { get; private set; }

            public Test2(ITest1 test1)
            {
                this.Test1 = test1;
            }
        }

        public class Test3
        {
            [Dependency]
            public ITest1 Test1 { get; set; }
        }
    }
}