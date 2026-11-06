namespace OneMoreFloor
{
    // Immutable story guests. They are never part of the ordinary random appearance pool.
    public sealed class FinaleDefinition
    {
        public readonly int LevelNumber,Appearance;
        public readonly string CharacterKey,VehicleKey,VehicleAsset;
        public string BoardedAsset { get { return VehicleAsset+"_boarded"; } }
        internal FinaleDefinition(int level,int appearance,string character,string vehicle,string asset)
        {LevelNumber=level;Appearance=appearance;CharacterKey=character;VehicleKey=vehicle;VehicleAsset=asset;}
    }
    public static class RooftopFinales
    {
        static readonly FinaleDefinition Alien=new FinaleDefinition(10,18,"finale.alien","vehicle.ufo","rooftop_ufo"),
            Aviator=new FinaleDefinition(20,19,"finale.aviator","vehicle.plane","rooftop_biplane"),
            Robot=new FinaleDefinition(30,20,"finale.robot","vehicle.rocket","rooftop_rocket"),
            Cat=new FinaleDefinition(40,21,"finale.cat","vehicle.broom","rooftop_broom"),
            Captain=new FinaleDefinition(50,22,"finale.captain","vehicle.airship","rooftop_airship"),
            Courier=new FinaleDefinition(60,23,"finale.courier","vehicle.ornithopter","rooftop_ornithopter"),
            Meteorologist=new FinaleDefinition(70,24,"finale.meteorologist","vehicle.balloon","rooftop_balloon"),
            Explorer=new FinaleDefinition(80,25,"finale.explorer","vehicle.gyrocopter","rooftop_gyrocopter"),
            Navigator=new FinaleDefinition(90,26,"finale.navigator","vehicle.sailboat","rooftop_sailboat"),
            Astronomer=new FinaleDefinition(100,27,"finale.astronomer","vehicle.moon","rooftop_moon");
        public static FinaleDefinition ForLevel(int level)
        {
            switch(level){case 10:return Alien;case 20:return Aviator;case 30:return Robot;case 40:return Cat;case 50:return Captain;case 60:return Courier;case 70:return Meteorologist;case 80:return Explorer;case 90:return Navigator;case 100:return Astronomer;default:return null;}
        }
        public static FinaleDefinition ForAppearance(int appearance)
        {return appearance>=18 && appearance<=27 ? ForLevel((appearance-17)*10) : null;}
    }
}
