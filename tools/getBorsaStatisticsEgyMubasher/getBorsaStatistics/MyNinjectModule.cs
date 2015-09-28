using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using Ninject.Modules;

namespace getBorsaStatistics
{
    public class MyNinjectModule : NinjectModule
    {

        public override void Load()
        {
            Bind<IGetLinks>().To<MubasherLinks>().Named("MubasherLinks");
            Bind<IGetLinks>().To<EgyptLinks>().Named("EgyptLinks");

        }
    }
}
