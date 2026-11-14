using System.Globalization;
namespace Projeta.Civil3D;

internal sealed record DrainBasin(string Name,double Hectares,double C);
internal sealed record DrainInput(IReadOnlyList<DrainBasin> Basins,double Intensity,double SlopePercent,double Manning,double Diameter,double MaxRatio,double MinVelocity,double MaxVelocity,double[] Diameters);
internal sealed record DrainSection(double Area,double Perimeter,double Capacity);
internal sealed record DrainCheck(double Diameter,double Capacity,double? Depth,double? Velocity,bool Acceptable);
internal sealed record DrainResult(double Flow,double Area,double WeightedC,DrainCheck Selected,DrainCheck? Suggested);
internal sealed record RainIdf(double K,double A,double B,double C,double MinMinutes,double MaxMinutes,double MinYears,double MaxYears,string Source)
{
    public double Intensity(double minutes,double years)
    {
        if(new[]{K,A,B,C,MinMinutes,MaxMinutes,MinYears,MaxYears,minutes,years}.Any(x=>!double.IsFinite(x)) || K<=0 || A<=0 || B<0 || C<=0 || MinMinutes<=0 || MaxMinutes<MinMinutes || MinYears<=0 || MaxYears<MinYears || string.IsNullOrWhiteSpace(Source))throw new ArgumentException("Confira os coeficientes, unidades, fonte e intervalos de validade da IDF.");
        if(minutes<MinMinutes||minutes>MaxMinutes||years<MinYears||years>MaxYears)throw new ArgumentException("Duração ou TR fora da faixa de validade informada da IDF. Não extrapolado.");
        var value=K*Math.Pow(years,A)/Math.Pow(minutes+B,C);
        if(!double.IsFinite(value)||value<=0)throw new ArgumentException("Intensidade IDF inválida.");
        return value;
    }
}
internal static class Drainage
{
    public static DrainSection Section(double diameter,double ratio,double n,double slope)
    {
        if(new[]{diameter,ratio,n,slope}.Any(x=>!double.IsFinite(x))||diameter<=0||ratio<0||ratio>1||n<=0||slope<=0)throw new ArgumentException("Geometria, rugosidade ou declividade inválidas.");
        if(ratio==0)return new(0,0,0);
        var theta=2*Math.Acos(1-2*ratio);
        var area=diameter*diameter/8*(theta-Math.Sin(theta));var perimeter=diameter*theta/2;
        return new(area,perimeter,area/n*Math.Pow(area/perimeter,2d/3)*Math.Sqrt(slope));
    }
    public static DrainResult Calculate(DrainInput x)
    {
        if(x.Basins.Count==0||x.Basins.Count>1000)throw new ArgumentException("Informe de 1 a 1.000 contribuições.");
        if(new[]{x.Intensity,x.SlopePercent,x.Manning,x.Diameter,x.MaxRatio,x.MinVelocity,x.MaxVelocity}.Any(v=>!double.IsFinite(v))||x.Intensity<=0||x.SlopePercent<=0||x.Manning<=0||x.Diameter<=0||x.MaxRatio<=0||x.MaxRatio>0.9||x.MinVelocity<0||x.MaxVelocity<0||(x.MaxVelocity>0&&x.MinVelocity>x.MaxVelocity))throw new ArgumentException("Informe chuva, declividade, n e diâmetro positivos; y/D até 0,90 e limites de velocidade coerentes.");
        foreach(var b in x.Basins)if(string.IsNullOrWhiteSpace(b.Name)||!double.IsFinite(b.Hectares)||b.Hectares<=0||!double.IsFinite(b.C)||b.C<0||b.C>1)throw new ArgumentException("Cada contribuição exige nome, área positiva em ha e C entre zero e um.");
        var area=x.Basins.Sum(b=>b.Hectares);
        if(area>400)throw new ArgumentException("Esta primeira ferramenta limita o método racional a 400 ha. Revise o método hidrológico para uma bacia maior.");
        var ca=x.Basins.Sum(b=>b.Hectares*b.C);var flow=x.Intensity*ca/360;
        DrainCheck Check(double diameter)
        {
            var cap=Section(diameter,x.MaxRatio,x.Manning,x.SlopePercent/100).Capacity;
            if(flow>cap*(1+1e-10))return new(diameter,cap,null,null,false);
            if(flow==0)return new(diameter,cap,0,0,x.MinVelocity==0);
            double lo=0,hi=x.MaxRatio;
            for(var i=0;i<90;i++){var mid=(lo+hi)/2;if(Section(diameter,mid,x.Manning,x.SlopePercent/100).Capacity<flow)lo=mid;else hi=mid;}
            var ratio=(lo+hi)/2;var velocity=flow/Section(diameter,ratio,x.Manning,x.SlopePercent/100).Area;
            return new(diameter,cap,ratio*diameter,velocity,(x.MinVelocity==0||velocity>=x.MinVelocity)&&(x.MaxVelocity==0||velocity<=x.MaxVelocity));
        }
        if(x.Diameters.Length==0||x.Diameters.Length>100||x.Diameters.Any(v=>!double.IsFinite(v)||v<=0))throw new ArgumentException("Informe de 1 a 100 diâmetros candidatos positivos, em metros.");
        var selected=Check(x.Diameter);DrainCheck? suggested=null;
        if(flow>0)foreach(var d in x.Diameters.Distinct().Order()){var check=Check(d);if(check.Acceptable){suggested=check;break;}}
        return new(flow,area,ca/area,selected,suggested);
    }
    public static (int Design,int Verification,string Note) OacCriteria(int roadWeight,int heightWeight,int occupationWeight)
    {
        if(!new[]{0,1,3}.Contains(roadWeight)||heightWeight<0||heightWeight>2||occupationWeight<0||occupationWeight>2)throw new ArgumentException("Classificação OAC inválida.");
        var sum=roadWeight+heightWeight+occupationWeight;
        return sum<=3?(25,50,"OAC classe I: soma até 3."):sum<=5?(50,100,"OAC classe II: soma 4 a 5."):(100,200,"OAC classe III: soma a partir de 6.");
    }
    public static string F(double x)=>x.ToString("F6",CultureInfo.GetCultureInfo("pt-BR"));
}
