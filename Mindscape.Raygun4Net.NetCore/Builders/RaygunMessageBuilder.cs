namespace Mindscape.Raygun4Net.Builders
{
    public class RaygunMessageBuilder : Raygun4Net.RaygunMessageBuilder
    {
        public static Raygun4Net.RaygunMessageBuilder New(RaygunSettingsBase settings)
        {
            return new RaygunMessageBuilder(settings);
        }

        private RaygunMessageBuilder(RaygunSettingsBase settings) : base(settings)
        {
        }
    }
}