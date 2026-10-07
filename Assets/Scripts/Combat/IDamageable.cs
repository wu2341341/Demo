public interface IDamageable
{
    bool TakeDamage(int damage);   //返回是否真正造成伤害
    bool IsDead { get; }
}