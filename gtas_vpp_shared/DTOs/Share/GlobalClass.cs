using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_shared.DTOs.Share
{
    public class GlobalClass
    {
        public sp_Authentication_Login UserInfo = new sp_Authentication_Login();
        public string? Server { get; set; }
        private int _busyCounter = 0;
        public bool isBusyPage
        {
            get => _busyCounter > 0;
            set
            {
                if (value)
                {
                    _busyCounter++;
                }
                else
                {
                    _busyCounter--;
                    if (_busyCounter < 0) _busyCounter = 0;
                }
            }
        }
        public string BaseUrl { get; set; } = string.Empty;
    }
}

