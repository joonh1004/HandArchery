namespace HandArchery.Core
{
    public readonly struct ShotCommand
    {
        public float AimX { get; }
        public float AimY { get; }
        public float Power { get; }
        public ShotCommand(float aimX, float aimY, float power)
        { AimX = aimX; AimY = aimY; Power = power; }
    }
}
