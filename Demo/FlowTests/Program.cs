using RedSea.Match3.Core;
using RedSea.Match3.Flow;

static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
var machine = new TurnStateMachine();
Check("starts idle", machine.State == GameState.Idle && !machine.InputLocked);
machine.Select(); Check("selecting accepts input", machine.State == GameState.Selecting && !machine.InputLocked);
machine.BeginResolve(); Check("resolving locks input", machine.State == GameState.Resolving && machine.InputLocked);
machine.BeginAnimation(); machine.BeginRefill(); machine.CheckChain(); machine.ReturnToIdle(); Check("stable board reopens input", machine.State == GameState.Idle && !machine.InputLocked);
machine.Finish(GameState.Win); Check("win is terminal", machine.State == GameState.Win && machine.InputLocked);
Check("terminal cannot pause", !machine.Pause() && machine.State == GameState.Win);
machine.Reset(); Check("reset returns to idle", machine.State == GameState.Idle && !machine.InputLocked);
machine.Select(); Check("pause locks selected input", machine.Pause() && machine.State == GameState.Paused && machine.InputLocked); Check("resume restores selected state", machine.Resume() && machine.State == GameState.Selecting && !machine.InputLocked);
machine.BeginResolve(); machine.BeginAnimation(); Check("pause preserves animation state", machine.Pause() && machine.State == GameState.Paused); Check("resume continues animation state", machine.Resume() && machine.State == GameState.Animating); machine.ReturnToIdle();
Console.WriteLine("FLOW CONTRACT TESTS PASSED");
