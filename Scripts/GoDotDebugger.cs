using Godot;

namespace Evix {

	public class GoDotDebugger {

		/// <summary>
		/// If the debugger is enabled.
		/// </summary>
		public bool isEnabled {
			get;
			private set;
		} = true;

		/// <summary>
		/// Make a new unity debugger. Override debug mode if you want
		/// </summary>
		public GoDotDebugger() { }
		public GoDotDebugger(bool isEnabled) {
			this.isEnabled = isEnabled;
		}

		/// <summary>
		/// Log a debug message
		/// </summary>
		/// <param name="debugMessage"></param>
		public void log(string debugMessage) {
			if (isEnabled) {
				GD.Print(debugMessage);
			}
		}

		/// <summary>
		/// Log a debug error
		/// </summary>
		/// <param name="debugMessage"></param>
		public void logError(string debugMessage) {
			if (isEnabled) {
				GD.PrintErr(debugMessage);
			}
		}
	}
}
