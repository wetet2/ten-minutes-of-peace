namespace PeacePark
{
    // Values also identify the drawing tabs and LAN submissions.
    public enum AnimalKind { Bear, Tiger, Rabbit, Frog, Giraffe, Elephant, Triceratops, Crocodile, Hippo, Gorilla }

    public static class AnimalKinds
    {
        static readonly string[] names = { "곰", "호랑이", "토끼", "개구리", "기린", "코끼리", "트리케라톱스", "악어", "하마", "고릴라" };
        public const int Count = 10;
        public static bool IsValid(int kind) => kind >= 0 && kind < Count;
        public static string Name(AnimalKind kind) => names[(int)kind];
        public static string RigResource(AnimalKind kind) =>
            kind == AnimalKind.Rabbit ? "Rabbit/RabbitRig" : "Animals/" + kind;
    }
}
