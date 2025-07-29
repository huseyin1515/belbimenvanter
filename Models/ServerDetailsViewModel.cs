using System.Collections.Generic;

namespace BelbimEnv.Models
{
    public class ServerDetailsViewModel
    {
        // Görüntülenen ana sunucu/cihaz bilgisi
        public Server Server { get; set; }

        // Bu cihaza BAĞLANAN diğer cihazların port bilgilerinin listesi
        public List<PortDetay> InboundConnections { get; set; }
    }
}