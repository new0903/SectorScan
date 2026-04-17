using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum NetworkStandard
    {
        Gsm,//2g
        Wcdma,//3g
        Lte,//4g
        Nr,//5g
    }

    public static class NSEnumerator
    {

        public static IEnumerable<NetworkStandard> GetNetwork
        {
            get
            {
                yield return NetworkStandard.Gsm;
                yield return NetworkStandard.Wcdma;
                yield return NetworkStandard.Lte;
                yield return NetworkStandard.Nr;
            }
        }
    }

}
