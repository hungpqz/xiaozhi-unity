using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XiaoZhi.Unity.IoT;
using XiaoZhi.Unity.MIoT;
using Application = UnityEngine.Device.Application;

namespace XiaoZhi.Unity
{
    public class MIoTSettingsUI : BaseUI
    {
        private GameObject _goLogin;
        private TMP_InputField _inputUserId;
        private TMP_InputField _inputPassword;
        private XButton _btnLogin;

        private GameObject _goVerify;
        private HyperlinkText _inputVerifyLink;
        private TMP_InputField _inputVerifyCode;
        private XButton _btnVerify;
        private XButton _btnReturn;

        private GameObject _goList;
        private Transform _listHome;
        private Transform _listRoom;
        private Transform _listDevice;
        private ThingMIoT _thingMIoT;
        private XButton _btnRefresh;

        private MiotHome[] _homes;
        private int _homeIndex;
        private MiotRoom[] _rooms;
        private int _roomIndex;
        private MiotDevice[] _devices;

        private string _verifyUrl;

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
            _inputPassword = GetComponent<TMP_InputField>(trLogin, "Password/InputField");
            _btnLogin = GetComponent<XButton>(trLogin, "Login/Button");
            _btnLogin.onClick.AddListener(() => OnClickLogin().Forget());
            _goVerify = GetGo(content, "VerifyUI");
            var trVerify = _goVerify.transform;
            _inputVerifyCode = GetComponent<TMP_InputField>(trVerify, "Code/InputField");
            _inputVerifyLink = GetComponent<HyperlinkText>(trVerify, "Code/Tips_Help/Text");
            _btnVerify = GetComponent<XButton>(trVerify, "Buttons/Verify");
            _btnVerify.onClick.AddListener(() => OnClickVerify().Forget());
            _btnReturn = GetComponent<XButton>(trVerify, "Buttons/Return");
            _btnReturn.onClick.AddListener(OnClickReturn);
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
            _thingMIoT = Context.ThingManager.GetThing<ThingMIoT>();
            _verifyUrl = null;
            OnLoginStateUpdate();
            await UniTask.CompletedTask;
        }

        private void OnLoginStateUpdate()
        {
            var isLogin = _thingMIoT.IsLogin;
            _goLogin.SetActive(!isLogin && string.IsNullOrEmpty(_verifyUrl));
            _goVerify.SetActive(!isLogin && !string.IsNullOrEmpty(_verifyUrl));
            _goList.SetActive(isLogin);
            if (isLogin) UpdateListUI();
            else if (string.IsNullOrEmpty(_verifyUrl)) UpdateLoginUI();
            else UpdateVerifyUI();
        }

        private void UpdateLoginUI()
        {
            _inputUserId.text = _thingMIoT.UserId;
            _inputPassword.text = "";
            _btnLogin.interactable = true;
        }

        private void UpdateVerifyUI()
        {
            _inputVerifyCode.text = "";
            _inputVerifyLink.OnClickLink.RemoveAllListeners();
            _inputVerifyLink.OnClickLink.AddListener(_ => Application.OpenURL(_verifyUrl));
            _btnVerify.interactable = true;
        }

        private void UpdateListUI()
        {
            UpdateHomes();
            _btnRefresh.interactable = true;
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
                var device = _devices[i];
                var go = _listDevice.GetChild(i).gameObject;
                go.SetActive(true);
                GetComponent<TMP_Text>(go.transform, "Text").text = device.Name;
                GetComponent<XSpriteChanger>(go.transform, "Icon").ChangeTo(device.IsOnline ? 0 : 1);
                var toggle = go.GetComponent<XToggle>();
                RemoveUniqueListener(toggle);
                toggle.isOn = device.IsOnline && _thingMIoT.IsWatchDevice(device.Did);
                toggle.interactable = device.IsOnline;
                AddUniqueListener(toggle, i, OnToggleDevice);
                var btnInfo = GetComponent<XButton>(go.transform, "BtnInfo");
                AddUniqueListener(btnInfo,
                    () => { ShowPopupUI<MIoTDeviceInfo>(new MIoTDeviceInfo.Data { Device = device }).Forget(); });
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

            var passToken = _inputPassword.text;
            if (string.IsNullOrEmpty(passToken))
            {
                await Context.UIManager.ShowNotificationUI((_inputPassword.placeholder as TMP_Text)!.text);
                return;
            }

            _btnLogin.interactable = false;
            var (success, error) = await _thingMIoT.Login(userId, passToken);
            if (success)
            {
                await ShowNotificationUI(Lang.GetRef("MIoT_LoginSuccess"));
            }
            else if (error.StartsWith("http"))
            {
                _verifyUrl = error;
            }
            else
            {
                await ShowNotificationUI(error);
            }

            _btnLogin.interactable = true;
            if (_thingMIoT.IsLogin)
            {
                await _thingMIoT.LoadDevices();
                OnLoginStateUpdate();
            }
            else if (!string.IsNullOrEmpty(_verifyUrl))
            {
                OnLoginStateUpdate();
            }
        }

        private async UniTask OnClickVerify()
        {
            var code = _inputVerifyCode.text;
            if (string.IsNullOrEmpty(code))
            {
                await Context.UIManager.ShowNotificationUI((_inputVerifyCode.placeholder as TMP_Text)!.text);
                return;
            }

            var (success, error) = await _thingMIoT.Verify(_verifyUrl, code);
            if (!success)
            {
                await ShowNotificationUI(error);
                return;
            }

            (success, error) = await _thingMIoT.Login(null, null);
            if (success) 
            {
                await ShowNotificationUI(Lang.GetRef("MIoT_LoginSuccess"));
            }
            else 
            {
                await ShowNotificationUI(error);
            }

            if (_thingMIoT.IsLogin)
            {
                await _thingMIoT.LoadDevices();
                OnLoginStateUpdate();
            }
        }

        private void OnClickReturn()
        {
            _verifyUrl = null;
            OnLoginStateUpdate();
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
            await ShowNotificationUI(Lang.GetRef("MIoT_RefreshSuccess"));
        }
    }
}