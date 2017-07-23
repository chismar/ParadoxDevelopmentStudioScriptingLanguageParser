using Sprache;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class ParadoxScriptParser
{
    public interface ISerializable
    {
        void SerializeTo(StringBuilder builder, int offset);
    }
    public class Id : ISerializable
    {
        public string Name;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append(Name);
        }
    }
    public class Ref : ISerializable
    {
        public string Name;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append("@").Append(Name);
        }
    }
    public class NumberValue : ISerializable
    {
        public float Value;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append(Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public class BooleanValue : ISerializable
    {
        public bool Value;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append(Value?"yes":"no");
        }
    }
    public class Percent : ISerializable
    {
        public float Value;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append(Value.ToString(CultureInfo.InvariantCulture)).Append("%%");
        }
    }
    public class Table : ISerializable
    {
        public List<Operator> Values;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append("{");

            foreach (var value in Values)
            {
                builder.AppendLine();
                (value as ISerializable).SerializeTo(builder, offset + 1);
            }
            if(Values.Count > 0)
                builder.AppendLine();
            builder.Append(' ', offset).Append("}");
        }
    }
    public class List : ISerializable
    {
        public List<object> Values;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append("{");
            foreach (var value in Values)
            {
                builder.Append(" ");
                (value as ISerializable).SerializeTo(builder, offset + 1);
            }
            builder.Append("}");
        }
    }
    public class StringValue : ISerializable
    {
        public string Value;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append('\"').Append(Value).Append('\"');
        }
    }
    public class Operator : ISerializable
    {
        public object Id;
        public enum Type { None, More, Less, Equals }
        public object Value;
        public Type Operation;
        void ISerializable.SerializeTo(StringBuilder builder, int offset)
        {
            builder.Append(' ', offset);
            (Id as ISerializable).SerializeTo(builder, offset);
            switch (Operation)
            {
                case Type.None:
                    break;
                case Type.More:
                    builder.Append(">");
                    break;
                case Type.Less:
                    builder.Append("<");
                    break;
                case Type.Equals:
                    builder.Append("=");
                    break;
            }
            var ser = Value as ISerializable;
            if (ser != null)
                ser.SerializeTo(builder, offset);
            else
                builder.Append(Value);

        }
    }
    public static Parser<string> Color = from start in Parse.String("0x") from rest in Parse.Chars("1234567890ABCDEFabcdef").Repeat(8) select string.Concat(rest);
    public static Parser<object> Comment = from commentStart in Parse.Char('#').AtLeastOnce() from text in Parse.AnyChar.Except(Parse.LineEnd.Or(Parse.LineTerminator)).Many() from lineEnd in Parse.LineEnd.Or(Parse.LineTerminator) select text; 
    public static Parser<object> Delimiter = Parse.Or(Parse.Select(Parse.Chars('\t', ' '), x => (object)x), Parse.LineEnd).Or(Comment).Many();
    public static Parser<string> TableOpen = from ldel in Delimiter from open in Parse.Char('{') from rdel in Delimiter select "";
    public static Parser<string> TableEnd = from ldel in Delimiter from open in Parse.Char('}') from rdel in Delimiter select "";
    public static Parser<Operator.Type> More = from str in Parse.String(">") select Operator.Type.More;
    public static Parser<Operator.Type> Less = from str in Parse.String("<") select Operator.Type.Less;
    public static Parser<Operator.Type> Equals = from str in Parse.String("=") select Operator.Type.Equals;
    public static Parser<Operator.Type> OpType = from ldel in Delimiter from opType in Equals.Or(More).Or(Less) from rdel in Delimiter select opType;

    public static Parser<BooleanValue> Yes = from str in Parse.String("yes") select new BooleanValue() { Value = true };
    public static Parser<BooleanValue> No = from str in Parse.String("no") select new BooleanValue() { Value = false };
    public static Parser<BooleanValue> Bool = from ldel in Delimiter from val in Yes.Or(No) from rdel in Delimiter select val;
    public static Parser<object> StringValueParser = from ldel in Delimiter
                                                          from strBegin in Parse.Char('\"')
                                                          from str in Parse.CharExcept("\"").Many().Select(x => string.Concat(x))
                                                          from strEnd in Parse.Char('\"')
                                                          from rdel in Delimiter
                                                          select new StringValue() { Value = str };
    
    public static Parser<Id> Identifier =
        from ldel in Delimiter
        from str in Parse.Chars("QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789_").AtLeastOnce().Select(x => string.Concat(x))
        from rdel in Delimiter
        select new Id() { Name = str };
    public static Parser<Ref> RefParser =
        from ldel in Delimiter
        from refSymbol in Parse.Char('@')
        from str in Parse.Chars("QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm0123456789_").AtLeastOnce().Select(x => string.Concat(x))
        from rdel in Delimiter
        select new Ref() { Name = str };
    public static Parser<object> IdParser = Parse.Or<object>(Identifier, RefParser);
    //public static Parser<string> IntNumber = from negation in Parse.Char('-').Optional() from number in Parse.Number select negation.IsEmpty ? "" : "-" + number;
    public static Parser<object> FloatNumber = from negation in Parse.Char('-').Optional() from number in Parse.DecimalInvariant select new NumberValue() { Value = negation.IsEmpty?float.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture) :-float.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture) };
    public static Parser<object> PercentNumber = from number in Parse.DecimalInvariant from per in Parse.Char('%').AtLeastOnce() select new Percent() {Value = float.Parse(number, NumberStyles.Float, CultureInfo.InvariantCulture) };

    //public static Parser<object> Number = IntNumber.Select(x => (object)x).Or(FloatNumber.Select(x => (object)x));
    public static Parser<object> ValueParser =
        from ldel in Delimiter
        from val in Color.Or(PercentNumber).Or(FloatNumber).Or(Bool).Or(StringValueParser).Or(IdParser)
        from rdel in Delimiter
        select val;
    public static Parser<List> ListParser =
        from start in TableOpen
        from vals in Parse.Ref(() => ValueParser.Many())
        from end in TableEnd
        select new List() { Values = new List<object>(vals) };

    public static Parser<object> ObjectValue =
        from ldel in Delimiter
        from obj in Parse.Or<object>(Parse.Ref(() => TableParser), Parse.Ref(() => ListParser))
        from rdel in Delimiter
        select obj;

    public static Parser<object> AnyValue =
        from ldel in Delimiter
        from any in Parse.Or(Parse.Ref(() => ValueParser), Parse.Ref(() => ObjectValue))
        from rdel in Delimiter
        select any;

    public static Parser<Operator> Op =
        from ldel in Delimiter
        from id in IdParser
        from opType in Parse.Ref(() => OpType)
        from value in Parse.Ref(() => AnyValue)
        from rdel in Delimiter
        select new Operator() { Id = id, Value = value, Operation = opType };

    public static Parser<List<Operator>> OpsList =
        from ops in Parse.Ref(() => Op).Many()
        select new List<Operator>(ops);

    public static Parser<Table> TableParser =
        from start in TableOpen
        from values in Parse.Ref(() => OpsList)
        from end in TableEnd
        select new Table() { Values = values };

    public static Table ParseTable(string text)
    {
        var value = TableParser.TryParse(text);
        if (value.WasSuccessful)
        {
            return value.Value;
        }
        else
            throw new System.Exception(string.Concat(value.Message) + " \nExpected: " + string.Concat(value.Expectations) + " \nRemainder: " + value.Remainder);
    }

    public static Operator ParseOp(string text)
    {
        var value = Op.TryParse(text);
        if (value.WasSuccessful)
        {
            return value.Value;
        }
        else
            throw new System.Exception(string.Concat(value.Message) + " \nExpected: " + string.Concat(value.Expectations) + " \nRemainder: " + value.Remainder);
    }

    public static List<Operator> ParseOps(string text)
    {
        var value = OpsList.TryParse(text);
        if (value.WasSuccessful)
        {
            return value.Value;
        }
        else
            throw new System.Exception(string.Concat(value.Message) + " \nExpected: " + string.Concat(value.Expectations) + " \nRemainder: " + value.Remainder);
    }
}
