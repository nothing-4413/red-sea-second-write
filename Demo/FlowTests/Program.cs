using RedSea.Match3.Core;
using RedSea.Match3.Flow;

static void Check(string name, bool value) { if (!value) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
var machine = new TurnStateMachine();
Check("starts idle", machine.State == GameState.Idle && !machine.InputLocked);
machine.Select(); Check("selecting accepts input", machine.State == GameState.Selecting && !machine.InputLocked);
machine.BeginResolve(); Check("resolving locks input", machine.State == GameState.Resolving && machine.InputLocked);
machine.BeginAnimation(); machine.BeginRefill(); machine.CheckChain(); machine.ReturnToIdle(); Check("stable board reopens input", machine.State == GameState.Idle && !machine.InputLocked);
machine.Finish(GameState.Win); Check("win is terminal", machine.State == GameState.Win && machine.InputLocked);
machine.Reset(); Check("reset returns to idle", machine.State == GameState.Idle && !machine.InputLocked);
Console.WriteLine("FLOW CONTRACT TESTS PASSED");
