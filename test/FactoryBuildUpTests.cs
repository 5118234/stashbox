﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stashbox.Attributes;
using Stashbox.Entity;

namespace Stashbox.Tests
{
    [TestClass]
    public class FactoryBuildUpTests
    {
        [TestMethod]
        public void FactoryBuildUpTests_DependencyResolve()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<ITest, Test>(context => context.WithFactory(() => new Test("test")));
                container.Register<ITest1, Test12>();

                var inst = container.Resolve<ITest1>();

                Assert.IsInstanceOfType(inst.Test, typeof(Test));
                Assert.AreEqual("test", inst.Test.Name);
            }
        }

        [TestMethod]
        public void FactoryBuildUpTests_DependencyResolve_ServiceUpdated()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<ITest, Test>(context => context.WithFactory(() => new Test("test")));
                container.Register<ITest2, Test2>();
                container.ReMap<ITest, Test>(context => context.WithFactory(() => new Test("test1")));
                var inst = container.Resolve<ITest2>();

                Assert.IsInstanceOfType(inst.Test, typeof(Test));
                Assert.AreEqual("test1", inst.Test.Name);
            }
        }

        [TestMethod]
        public void FactoryBuildUpTests_Resolve()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<ITest, Test>(context => context.WithFactory(() => new Test("test")));
                container.Register<ITest1, Test1>();

                var inst = container.Resolve<ITest1>();

                Assert.IsInstanceOfType(inst.Test, typeof(Test));
                Assert.AreEqual("test", inst.Test.Name);
            }
        }

        [TestMethod]
        public void FactoryBuildUpTests_Resolve_NotSame()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<ITest, Test>(context => context.WithInjectionParameters(new InjectionParameter { Value = "test", Name = "name" }));
                container.Register<ITest1>(context => context.WithFactory(cont =>
                {
                    var test1 = cont.Resolve<ITest>();
                    return new Test12(test1);
                }));

                var inst1 = container.Resolve<ITest1>();
                var inst2 = container.Resolve<ITest1>();

                Assert.AreNotSame(inst1.Test, inst2.Test);
            }
        }

        [TestMethod]
        public void FactoryBuildUpTests_Resolve_ContainerFactory()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<Test3>();
                container.Register<ITest>(context => context.WithFactory(c => c.Resolve<Test3>()));

                var inst = container.Resolve<ITest>();

                Assert.IsInstanceOfType(inst, typeof(Test3));
            }
        }

        [TestMethod]
        public void FactoryBuildUpTests_Resolve_ContainerFactory_Constructor()
        {
            using (var container = new StashboxContainer())
            {
                container.Register<Test3>();
                container.Register<ITest1, Test12>();
                container.Register(typeof(ITest), context => context.WithFactory(c => c.Resolve<Test3>()));

                var test1 = container.Resolve<ITest1>();
                Assert.IsInstanceOfType(test1.Test, typeof(Test3));
            }
        }

        public interface ITest { string Name { get; } }

        public interface ITest1 { ITest Test { get; } }

        public interface ITest2 { ITest Test { get; } }

        public class Test3 : ITest
        {
            public string Name { get; }
        }

        public class Test : ITest
        {
            public string Name { get; }

            public Test(string name)
            {
                this.Name = name;
            }
        }

        public class Test2 : ITest2
        {
            [Dependency]
            public ITest Test { get; set; }
        }

        public class Test1 : ITest1
        {
            public ITest Test { get; set; }

            [InjectionMethod]
            public void Init(ITest test)
            {
                this.Test = test;
            }
        }

        public class Test12 : ITest1
        {
            public ITest Test { get; set; }

            public Test12(ITest test)
            {
                this.Test = test;
            }
        }
    }
}
