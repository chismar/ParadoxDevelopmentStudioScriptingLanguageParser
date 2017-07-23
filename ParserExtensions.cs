
using System.Collections.Generic;
using static ParadoxScriptParser;

public static class ParserExtensions
{
    public static Table Table(this Operator op) => op.Value as Table;
    public static Table Table(this Operator op, string name) => op.Value(name)?.Table();
    public static Operator Value(this Table table, string name) => table.Values.Find(x => (x.Id as Id)?.Name == name);
    public static Operator Value(this Operator op, string name) => op.Table()?.Values.Find(x => (x.Id as Id)?.Name == name);
    public static List<Operator> Values(this Operator op, string name) => op.Table()?.Values.FindAll(x => (x.Id as Id)?.Name == name);
    public static List<Operator> Values(this Operator op) => op.Table()?.Values;


    public static Operator Value(this List<Operator> ops, string name) => ops.Find(x => (x.Id as Id)?.Name == name);
    public static List<Operator> Values(this List<Operator> ops, string name) => ops.FindAll(x => (x.Id as Id)?.Name == name);


    public static float? Percent(this Operator op) => (op.Value as Percent)?.Value;
    public static float? Float(this Operator op) => (op.Value as NumberValue)?.Value;
    public static bool? Bool(this Operator op) => (op.Value as BooleanValue)?.Value;
    public static List<object> List(this Operator op) => (op.Value as List)?.Values;
    public static string String(this Operator op) => (op.Value as StringValue)?.Value;
    public static string Id(this Operator op) => (op.Value as Id)?.Name;
    public static string Ref(this Operator op) => (op.Value as Ref)?.Name;


    public static float? Percent(this Operator op, string name) => (op.Value(name)?.Value as Percent)?.Value;
    public static float? Float(this Operator op, string name) => (op.Value(name)?.Value as NumberValue)?.Value;
    public static bool? Bool(this Operator op, string name) => (op.Value(name)?.Value as BooleanValue)?.Value;
    public static List<object> List(this Operator op, string name) => (op.Value(name)?.Value as List)?.Values;
    public static string String(this Operator op, string name) => (op.Value(name)?.Value as StringValue)?.Value;
    public static string Id(this Operator op, string name) => (op.Value(name)?.Value as Id)?.Name;
    public static string Ref(this Operator op, string name) => (op.Value(name)?.Value as Ref)?.Name;

    public delegate T FromTo<P, T>(P from);
    public static List<T> To<P,T>(this List<P> list, FromTo<P,T> fromTo)
    {
        List<T> toList = new List<T>();
        foreach (var p in list)
            toList.Add(fromTo(p));
        return toList;
    }

}
