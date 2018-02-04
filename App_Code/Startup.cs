using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebSite15.Startup))]
namespace WebSite15
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
