using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScottWatch.Models
{
    public class PartsMaster
    {
        public string PM_VEN { get; set; }
        public string PM_PRT { get; set; }
        public string PM_DES { get; set; }
        public string PM_WT { get; set; }
        public string PM_OH { get; set; }
        public string PM_QA { get; set; }
        public string PM_QR { get; set; }
        public string PM_DTA { get; set; }
        public string PM_NET { get; set; }
        public string NetOnHand { get; set; }

        public string PM_BR { get; set; }

    }

    public class PutOrders
    {
        public string PH_PO { get; set; }
        public string PH_PRT { get; set; }
        public string PH_QTR { get; set; }
        public string PH_VEN { get; set; }
        public string PH_DTR { get; set; }
        public string PH_BR { get; set; }

    }

    public class PickOrders
    {
        public string SO_ORD { get; set; }
        public string SD_LNE { get; set; }
        public string SD_PRT { get; set; }
        public string SD_BIN { get; set; }
        public string SD_TISS { get; set; }
        public string SO_DTO { get; set; }
        public string SD_TMO { get; set; }
        public string SD_PO { get; set; }
        public string SO_BR { get; set; }


    }
}
