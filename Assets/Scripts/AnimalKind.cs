namespace PeacePark
{
    // Values also identify the drawing tabs and LAN submissions.
    public enum AnimalKind { Bear, Tiger, Rabbit, Frog, Giraffe, Elephant, Triceratops }

    public static class AnimalKinds
    {
        static readonly string[] names = { "곰", "호랑이", "토끼", "개구리", "기린", "코끼리", "트리케라톱스" };
        public const int Count = 7;
        public static bool IsValid(int kind) => kind >= 0 && kind < Count;
        public static string Name(AnimalKind kind) => names[(int)kind];
        public static string RigResource(AnimalKind kind) =>
            kind == AnimalKind.Rabbit ? "Rabbit/RabbitRig" : "Animals/" + kind;
    }
}
