namespace Game.GridPrototype
{
    // Countdown uses real time, so a paused menu or unfocused window cannot stop rollback.
    public sealed class DisplayChangePreview
    {
        public bool Active { get; private set; }
        public bool Original { get; private set; }
        public bool Requested { get; private set; }
        public float Deadline { get; private set; }
        public void Begin(bool original,bool requested,float now){Original=original;Requested=requested;Deadline=now+15;Active=true;}
        public bool Expired(float now)=>Active&&now>=Deadline;
        public void End()=>Active=false;
    }
}
