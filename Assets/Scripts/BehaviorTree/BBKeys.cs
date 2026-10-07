namespace BehaviorTree
{
    //黑板键常量：防止字符串拼错
    public static class BBKeys
    {
        //通用
        public const string Player = "player";
        public const string Transform = "transform";
        public const string Animator = "animator";
        public const string CharacterController = "characterController";

        //Boss 属性
        public const string MoveSpeed = "moveSpeed";
        public const string AttackRange = "attackRange";
        public const string AttackDamage = "attackDamage";
        public const string AttackCooldown = "attackCooldown";
        public const string DetectionRange = "detectionRange";
        public const string Phase = "phase";

        //状态
        public const string IsAttacking = "isAttacking";
        public const string IsStunned = "isStunned";

        //召唤
        public const string SummonCooldown = "summonCooldown";
        public const string MaxMinions = "maxMinions";
        public const string SummonRange = "summonRange";
    }
}