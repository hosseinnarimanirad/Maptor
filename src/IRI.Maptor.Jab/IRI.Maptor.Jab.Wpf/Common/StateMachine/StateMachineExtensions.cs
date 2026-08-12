//using System.Windows.Input;
//

//namespace Stateless;

//public static class StateMachineExtensions
//{
//    public static ICommand CreateCommand<TState, TTrigger>(this StateMachine<TState, TTrigger> stateMachine, TTrigger trigger)
//    {
//        return new RelayCommand
//          (
//            (param) => stateMachine.Fire(trigger),
//            (param) => stateMachine.CanFire(trigger)
//          );
//    }
//}
