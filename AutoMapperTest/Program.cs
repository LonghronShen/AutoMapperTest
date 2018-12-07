using AutoMapper;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AutoMapperTest
{

    internal class AModel
    {

        public string Field1 { get; set; }

        public string Field2 { get; set; }

        public override string ToString()
        {
            return $"A: Field1 = {this.Field1}, Field2 = {this.Field2}";
        }

    }

    internal class BModel
    {

        public string Field3 { get; set; }

        public string Field4 { get; set; }

        public override string ToString()
        {
            return $"B: Field1 = {this.Field3}, Field2 = {this.Field4}";
        }

    }

    internal class CModel
    {

        public string Field5 { get; set; }

        public string Field6 { get; set; }

        public override string ToString()
        {
            return $"C: Field1 = {this.Field5}, Field2 = {this.Field6}";
        }

    }

    public static class TupleMapper
    {

        public static object Map(object source, Type tupleType)
        {
            if (tupleType.GetInterface(nameof(ITuple), true) == null)
            {
                throw new InvalidOperationException($"Type {tupleType.FullName} is not a Tuple type.");
            }
            var itemTypes = tupleType.GenericTypeArguments;
            var values = itemTypes.Select(itemType =>
                Mapper.Map(source, source.GetType(), itemType)).ToArray();
            return Activator.CreateInstance(tupleType, values);
        }

        public static T Map<T>(object source) where T : ITuple
        {
            var itemTypes = typeof(T).GenericTypeArguments;
            var values = itemTypes.Select(itemType =>
                Mapper.Map(source, source.GetType(), itemType)).ToArray();
            return (T)Activator.CreateInstance(typeof(T), values);
        }

        public static dynamic MapDynamic(object source, params Type[] itemTypes)
        {
            var dict = itemTypes.Select((itemType, i) => new
            {
                Key = $"Item{i + 1}",
                Value = Mapper.Map(source, source.GetType(), itemType)
            }).ToDictionary(x => x.Key, x => x.Value);
            return (dynamic)dict.Aggregate(
                new ExpandoObject() as IDictionary<string, object>, (a, p) =>
                {
                    a.Add(p.Key, p.Value);
                    return a;
                });
        }

    }

    internal class MyProfile
        : Profile
    {

        public MyProfile()
        {
            this.CreateMap<AModel, BModel>()
                .ForMember(x => x.Field3, x => x.MapFrom(z => z.Field1 + "_b"))
                .ForMember(x => x.Field4, x => x.MapFrom(z => z.Field2 + "_b"));
            this.CreateMap<AModel, CModel>()
                .ForMember(x => x.Field5, x => x.MapFrom(z => z.Field1 + "_c"))
                .ForMember(x => x.Field6, x => x.MapFrom(z => z.Field2 + "_c"));
        }

    }

    internal class Program
    {

        private static void Main(string[] args)
        {
            Mapper.Initialize(config => config.AddProfile<MyProfile>());

            var a = new AModel()
            {
                Field1 = "a",
                Field2 = "b"
            };

            // Map to any type of Tuple, and then auto destructs.
            var (b1, b2) = TupleMapper.Map<Tuple<BModel, BModel>>(a);

            // Any number no more than 9 of items is ok.
            var (b3, b4, c1) = TupleMapper.Map<Tuple<BModel, BModel, CModel>>(a);

            // Someone may ask why we cannot use Mapper.Map.
            // It is just beacuse AutoMapper's mapping registration is static since compilation time.
            // So we should write our own dynamic mapping function to do so.
            // Another related problem is, what if we use a dynamically created object to do the destruction?
            // The question is partially answered in the examples below.
            // We will provide a way to make a dynamic object which contains what you want.
            // Sadly, since the dynamically created object's type is kwnown at runtime,
            // and the destruction assignment syntax only works in compilation time,
            // we cannot use a strongly-typed way to do so.

            Console.WriteLine("Result 1:");
            Console.WriteLine($"b1: {b1.ToString()}");
            Console.WriteLine($"b2: {b2.ToString()}");

            Console.WriteLine("Result 2:");
            Console.WriteLine($"b3: {b3.ToString()}");
            Console.WriteLine($"b4: {b4.ToString()}");
            Console.WriteLine($"c1: {c1.ToString()}");

            // If you want to map as many as possible destinations, do like this.
            // This way you can get more than 9 items while Tuple just holds no more than 9 items.
            var destination = TupleMapper.MapDynamic(a,
                typeof(BModel), typeof(BModel), typeof(CModel), typeof(CModel));
            Console.WriteLine("Result 3:");
            Console.WriteLine($"Item1: {destination.Item1.ToString()}");
            Console.WriteLine($"Item2: {destination.Item2.ToString()}");
            Console.WriteLine($"Item3: {destination.Item3.ToString()}");
            Console.WriteLine($"Item4: {destination.Item4.ToString()}");

            // A more effetive way is, why not just use Linq to get a destination collection?
            var destinations = new[]
            {
                typeof(BModel), typeof(BModel), typeof(CModel), typeof(CModel)
            }.Select(x => Mapper.Map(a, a.GetType(), x)).ToList();
            Console.WriteLine("Result 4:");
            destinations.ForEach(x => Console.WriteLine("Item ?:" + x.ToString()));

            Console.ReadKey();
        }

    }

}
