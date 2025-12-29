namespace ProjetDelta.Data.DataClasses
{
    public class Attributs
    {
        private float _intel = 0;
        private float _force = 0;
        private float _constitution = 0;
        private float _dex = 0;
        private int _darkness = 0;

        private float _mulIntel = 1;
        private float _mulForce = 1;
        private float _mulConst = 1;
        private float _mulDex = 1;

        public Attributs()
        {
            
        }

        public float GetIntel() => _intel * _mulIntel;
        public float GetForce() => _force * _mulForce;
        public float GetConstitution() => _constitution * _mulConst;
        public float GetDex() => _dex * _mulDex;
        public int GetDarkness() => _darkness;

        //J'utilise des gets car dto, sinons j'aurais créé une fonction dans le dto pour retourner les valeurs
        public Attributs AddAttributes(AttributsDto attri)
        {
            _intel += attri.Intel;
            _force += attri.Force;
            _constitution += attri.Constitution;
            _dex += attri.Dex;
            return this;
        }
        
        //public void SetAttributes(AttributsDto attri)
        //{
        //    _intel = attri.Intel;
        //    _force = attri.Force;
        //    _constitution = attri.Constitution;
        //    _dex = attri.Dex;
        //}
        //public Attributs ModifyMultipliers(AttributsDto attributs)
        //{
        //    attributs.MulIntel *= _mulIntel;
        //    attributs.MulForce *= _mulForce;
        //    attributs.MulConst *= _mulConst;
        //    attributs.MulDex *= _mulDex;
        //    return this;
        //}
        public Attributs ModifyMultipliers(AttributsDto attributs)
        {
            _mulIntel *= attributs.MulIntel;
            _mulForce *= attributs.MulForce;
            _mulConst *= attributs.MulConst;
            _mulDex *= attributs.MulDex;
            return this;
        }
        //mappeur
        public AttributsDto ToDto()
        {
            return new AttributsDto
            {
                Intel = _intel,
                Force = _force,
                Constitution = _constitution,
                Dex = _dex,
                Darkness = _darkness,
                MulDex = _mulDex,
                MulConst = _mulConst,
                MulForce = _mulForce,
                MulIntel = _mulIntel,
                
            };
        }
    }
}
