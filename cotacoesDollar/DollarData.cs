using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace cotacoesDollar
{
    [DataContract]
    class DollarData
    {
        [DataMember]
        public string AtualValue { get; set; } // valor atual
        [DataMember]
        public string AtualPercent { get; set; } // porcentagem de descida/subida
        [DataMember]
        public string Date  { get; set; } // data da dotação
        [DataMember]
        public string TypeName  { get; set; } // tipo de dollar 
    }
}
