using Sandbox.UI;

public class DeathCounter : Panel
{
	public List<Death> Deaths = new(3);
	
	public DeathCounter()
	{
		for ( var i = 0; i < 3; i++ )
		{
			var death = new Death();
			Deaths.Add(death);
			AddChild( death );
		}
	}
	
	public Death this[int i] => Deaths[i];
}
