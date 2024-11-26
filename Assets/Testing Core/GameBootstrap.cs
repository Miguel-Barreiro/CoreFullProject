using Core.Initialization;
using Testing_Core.ComponentSystems;
using Testing_Core.EntitySystems;
using Testing_Core.Model;
using Testing_Core.View.UI;
using UnityEngine;

namespace Testing_Core
{
    public class GameBootstrap : SceneInstaller
    {
        [SerializeField] private GameConfig GameConfig;
        [SerializeField] private UIViewA UIView;

        
        protected override void Instantiate()
        {
            GameModel gameModel = new GameModel();
            BindInstance(gameModel);
            
            StartGameSystem startGameSystem = new StartGameSystem();
            BindInstance(startGameSystem);
            
            BindInstance(GameConfig);
            
            BindInstance(new AliveComponentSystem());
            
            BindInstance(new PrioritySystemTest());

            
            

            // RegisterUIScreenDefinition();

            UIMessengerA uiMessengerA = new UIMessengerA();
            UIControllerMultiListener uiControllerMultiListener = new UIControllerMultiListener();
            BindInstance(uiControllerMultiListener);

            uiControllerMultiListener.Register(uiMessengerA);
            UIView.Register(uiMessengerA);
        }
    }
}