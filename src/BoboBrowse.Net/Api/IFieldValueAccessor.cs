namespace BoboBrowse.Api
{
	public interface IFieldValueAccessor
	{
		string GetFormatedValue(int index);
		object GetRawValue(int index);
	}
}