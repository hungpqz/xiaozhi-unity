using Cysharp.Threading.Tasks;
using MIoT;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoZhi.Unity.IoT;

namespace XiaoZhi.Unity
{
    public class MIoTSettingsUI : BaseUI
    {
        private GameObject _goLogin;
        private TMP_InputField _inputUserId;
        private TMP_InputField _inputPassToken;
        private XButton _btnLogin;

        private GameObject _goList;
        private Transform _listHome;
        private Transform _listRoom;
        private Transform _listDevice;
        private ThingMiot _thingMIoT;
        private XButton _btnRefresh;

        private MiotHome[] _homes;
        private int _homeIndex;
        private MiotRoom[] _rooms;
        private int _roomIndex;
        private MiotDevice[] _devices;

        public override string GetResourcePath()
        {
            return "Assets/Res/UI/SettingsUI/MIoT.prefab";
        }

        protected override void OnInit()
        {
            var content = Tr.Find("Viewport/Content");
            _goLogin = GetGo(content, "LoginUI");
            var trLogin = _goLogin.transform;
            _inputUserId = GetComponent<TMP_InputField>(trLogin, "UserId/InputField");
            _inputPassToken = GetComponent<TMP_InputField>(trLogin, "PassToken/InputField");
            _btnLogin = GetComponent<XButton>(trLogin, "Login/Button");
            _btnLogin.onClick.AddListener(() => OnClickLogin().Forget());
            _goList = GetGo(content, "ListUI");
            var trList = _goList.transform;
            _listHome = trList.Find("HomeList/List");
            _listRoom = trList.Find("RoomList/List");
            _listDevice = trList.Find("DeviceList");
            GetComponent<XButton>(trList, "Buttons/BtnClear").onClick.AddListener(OnClickClear);
            GetComponent<XButton>(trList, "Buttons/BtnLogout").onClick.AddListener(OnClickLogout);
            _btnRefresh = GetComponent<XButton>(trList, "Buttons/BtnRefresh");
            _btnRefresh.onClick.AddListener(() => OnClickRefresh().Forget());
        }

        protected override async UniTask OnShow(BaseUIData data = null)
        {
            _thingMIoT = Context.ThingManager.GetThing<ThingMiot>();
            _btnLogin.interactable = true;
            _btnRefresh.interactable = true;
            OnLoginStateUpdate();
            await UniTask.CompletedTask;
        }

        private void OnLoginStateUpdate()
        {
            var isLogin = _thingMIoT.IsLogin;
            _goLogin.SetActive(!isLogin);
            _goList.SetActive(isLogin);
            if (!isLogin) UpdateLoginUI();
            else UpdateListUI();
        }

        private void UpdateLoginUI()
        {
            UpdateUserId();
            UpdatePassToken();
            _btnLogin.interactable = true;
        }

        private void UpdateListUI()
        {
            UpdateHomes();
        }

        private void UpdateUserId()
        {
            _inputUserId.text = _thingMIoT.UserId;
        }

        private void UpdatePassToken()
        {
            _inputPassToken.text = _thingMIoT.PassToken;
        }

        private void UpdateHomes()
        {
            _homes = _thingMIoT.GetHomes();
            Tools.EnsureChildren(_listHome, _homes.Length);
            for (var i = 0; i < _homes.Length; i++)
            {
                var go = _listHome.GetChild(i).gameObject;
                go.SetActive(true);
                GetComponent<TMP_Text>(go.transform, "Text").text = _homes[i].Name;
                var toggle = go.GetComponent<XToggle>();
                RemoveUniqueListener(toggle);
                toggle.isOn = i == _homeIndex;
                AddUniqueListener(toggle, i, OnToggleHome);
            }
            
            SelectHome(_homeIndex, true);
        }

        private void OnToggleHome(Toggle toggle, int index, bool isOn)
        {
            if (isOn) SelectHome(index);
        }

        private void SelectHome(int index, bool force = false)
        {
            if (_homeIndex == index && !force) return;
            _homeIndex = index;
            UpdateRooms();
        }

        private void UpdateRooms()
        {
            _rooms = _thingMIoT.GetRooms(_homes[_homeIndex].Id);
            Tools.EnsureChildren(_listRoom, _rooms.Length);
            for (var i = 0; i < _rooms.Length; i++)
            {
                var go = _listRoom.GetChild(i).gameObject;
                go.SetActive(true);
                GetComponent<TMP_Text>(go.transform, "Text").text = _rooms[i].Name;
                var toggle = go.GetComponent<XToggle>();
                RemoveUniqueListener(toggle);
                toggle.isOn = i == _roomIndex;
                AddUniqueListener(toggle, i, OnToggleRoom);
            }
            
            SelectRoom(_roomIndex, true);
        }

        private void OnToggleRoom(Toggle toggle, int index, bool isOn)
        {
            if (isOn) SelectRoom(index);
        }

        private void SelectRoom(int index, bool force = false)
        {
            if (_roomIndex == index && !force) return;
            _roomIndex = index;
            UpdateDevices();
        }

        private void UpdateDevices()
        {
            _devices = _thingMIoT.GetDevices(_rooms[_roomIndex].Id);
            Tools.EnsureChildren(_listDevice, _devices.Length);
            for (var i = 0; i < _devices.Length; i++)
            {
                var go = _listDevice.GetChild(i).gameObject;
                go.SetActive(true);
                GetComponent<TMP_Text>(go.transform, "Text").text = _devices[i].Name;
                GetComponent<XSpriteChanger>(go.transform, "Icon").ChangeTo(_devices[i].IsOnline ? 0 : 1);
                var toggle = go.GetComponent<XToggle>();
                RemoveUniqueListener(toggle);
                toggle.isOn = _devices[i].IsOnline && _thingMIoT.IsWatchDevice(_devices[i].Did);
                toggle.interactable = _devices[i].IsOnline;
                AddUniqueListener(toggle, i, OnToggleDevice);
            }
        }

        private void OnToggleDevice(Toggle toggle, int index, bool isOn)
        {
            if (isOn)
            {
                _thingMIoT.WatchDevice(_devices[index].Did);
            }
            else
            {
                _thingMIoT.UnwatchDevice(_devices[index].Did);
            }
        }

        private async UniTask OnClickLogin()
        {
            var userId = _inputUserId.text;
            if (string.IsNullOrEmpty(userId))
            {
                await Context.UIManager.ShowNotificationUI((_inputUserId.placeholder as TMP_Text)!.text);
                return;
            }

            var passToken = _inputPassToken.text;
            if (string.IsNullOrEmpty(passToken))
            {
                await Context.UIManager.ShowNotificationUI((_inputPassToken.placeholder as TMP_Text)!.text);
                return;
            }

            _btnLogin.interactable = false;
            var success = await _thingMIoT.Login("cn", _inputUserId.text, _inputPassToken.text);
            await ShowNotificationUI(Lang.GetRef(success ? "MIoT_LoginSuccess" : "MIoT_LoginFailed"));
            _btnLogin.interactable = true;
            if (success)
            {
                await _thingMIoT.LoadDevices();
                OnLoginStateUpdate();
            }
        }

        private void OnClickLogout()
        {
            _thingMIoT.Logout();
            OnLoginStateUpdate();
        }

        private void OnClickClear()
        {
            _thingMIoT.UnwatchAllDevices();
            UpdateDevices();
        }

        private async UniTask OnClickRefresh()
        {
            _btnRefresh.interactable = false;
            await _thingMIoT.LoadDevices();
            UpdateListUI();
            _btnRefresh.interactable = true;
        }
    }
}