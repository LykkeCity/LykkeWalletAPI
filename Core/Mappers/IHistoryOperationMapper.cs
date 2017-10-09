namespace Core.Mappers
{
    public interface IHistoryOperationMapper<out TResult, in TSource>
    {
        TResult Map(TSource source);
    }
}
