namespace MoexPortfolioSimulator.Data.Providers
{

    public class FinamSecurity
    {
        private string _id;

        private string _name;

        private string _code;

        private string _market;

        private string _marketId;

        private string _decp;

        private string _emitentChild;

        private string _url;

        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Code
        {
            get { return _code; }
            set { _code = value; }
        }

        public string Market
        {
            get { return _market; }
            set { _market = value; }
        }

        public string MarketId
        {
            get { return _marketId; }
            set { _marketId = value; }
        }

        public string Decp
        {
            get { return _decp; }
            set { _decp = value; }
        }

        public string EmitentChild
        {
            get { return _emitentChild; }
            set { _emitentChild = value; }
        }

        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }
    }
}