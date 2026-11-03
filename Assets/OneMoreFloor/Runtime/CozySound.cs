using System;
namespace OneMoreFloor
{
    // Original, deterministic PCM cues; no downloads, random gameplay state or sharp transients.
    public static class CozySound
    {
        public static float[] Bell(int rate=44100)
        {
            if(rate<8000)throw new ArgumentOutOfRangeException(nameof(rate));
            var samples=new float[(int)(rate*.82)];
            for(int i=0;i<samples.Length;i++)
            {
                double t=(double)i/rate,attack=1-Math.Exp(-t/0.012),release=Fade(t,.64,.82);
                double tone=Math.Sin(2*Math.PI*880*t)*Math.Exp(-t/0.23)
                    +.32*Math.Sin(2*Math.PI*1320*t)*Math.Exp(-t/0.16)
                    +.12*Math.Sin(2*Math.PI*2200*t)*Math.Exp(-t/0.09);
                samples[i]=(float)(tone*attack*release*.12);
            }
            return samples;
        }
        public static float[] Celebration(int rate=44100)
        {
            if(rate<8000)throw new ArgumentOutOfRangeException(nameof(rate));
            var samples=new float[(int)(rate*1.25)];uint noise=0x43A7u;double filtered=0;
            double[] notes={783.99,987.77,1174.66,1567.98};
            for(int i=0;i<samples.Length;i++)
            {
                double t=(double)i/rate;
                unchecked { noise=noise*1664525u+1013904223u; }
                filtered+=(((noise>>8)/8388607.5-1)-filtered)*.035;
                // Quiet airy release, rather than a clap, impact or explosion.
                double airy=filtered*Math.Sin(Math.PI*Math.Min(1,t/.46))*.035*Math.Exp(-t/0.5),tone=0;
                for(int n=0;n<notes.Length;n++)
                {
                    double a=t-.075-n*.075;if(a<0)continue;
                    double env=(1-Math.Exp(-a/0.018))*Math.Exp(-a/0.22);
                    tone+=Math.Sin(2*Math.PI*notes[n]*a)*env*.044;
                }
                samples[i]=(float)((airy+tone)*Fade(t,1,1.25));
            }
            return samples;
        }
        static double Fade(double t,double begin,double end)
        {double x=Math.Max(0,Math.Min(1,(end-t)/(end-begin)));return x*x*(3-2*x);}
    }
}
