using System;
using Newtonsoft.Json;

public class Week
{
    public string name;
    public DateTime dayStart;
    public DateTime dayEnd;    
    public bool isVacation = false;
    
    [JsonIgnore]
    public string ptbrName
    {
        get
        {
            return name.Replace("Week", "Semana")
            .Replace("January", "Janeiro")
            .Replace("February", "Fevereiro")
            .Replace("March", "Março")
            .Replace("April", "Abril")
            .Replace("May", "Maio")
            .Replace("June", "Junho")
            .Replace("July", "Julho")
            .Replace("August", "Agosto")
            .Replace("September", "Setembro")
            .Replace("October", "Outubro")
            .Replace("November", "Novembro")
            .Replace("December", "Dezembro");
        }
    }

    [JsonIgnore]
    public string startEndString => $"{dayStart.ToString("dd/MM/yy")} - {dayEnd.ToString("dd/MM/yy")}";

    public Week(string name, DateTime dayStart, DateTime dayEnd, bool isVacation = false)
    {
        this.name = name;
        this.dayStart = dayStart;
        this.dayEnd = dayEnd;
        this.isVacation = isVacation;
    }
}