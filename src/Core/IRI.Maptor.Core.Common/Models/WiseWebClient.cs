using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRI.Maptor.Core.Common.Model
{
    public class WiseWebClient : System.Net.WebClient
    {
        int _timeout;

        public WiseWebClient(int timeout = 30)
        {
            _timeout = timeout;
        }

        protected override System.Net.WebRequest GetWebRequest(Uri address)
        {
            var result = base.GetWebRequest(address);

            result.Timeout = _timeout;

            return result;
        }
    }
}
