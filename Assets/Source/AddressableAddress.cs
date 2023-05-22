using System.Linq;
using System.Net;

namespace Source
{
    public class AddressableAddress
    {
        public static string AddressableUrl {
            get
            {
                return Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.First(
                        f => f.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .ToString();
            }
        }
    }
}