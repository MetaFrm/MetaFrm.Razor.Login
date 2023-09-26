using MetaFrm.Api.Models;
using MetaFrm.Auth;
using MetaFrm.Control;
using MetaFrm.Razor.ViewModels;
using MetaFrm.Service;
using MetaFrm.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace MetaFrm.Razor
{
    /// <summary>
    /// Login
    /// </summary>
    public partial class Login
    {
        internal LoginViewModel LoginViewModel = Factory.CreateViewModel<LoginViewModel>();

        private bool isBusy = true;

        private bool rememberme = true;
        /// <summary>
        /// Rememberme
        /// </summary>
        public bool Rememberme 
        {
            get
            {
                return this.rememberme;
            }
            set
            {
                if (this.rememberme != value)
                {
                    this.rememberme = value;

                    if (!this.rememberme)
                        this.AutoLogin = false;
                }
            }
        }

        private bool autoLogin = false;
        /// <summary>
        /// AutoLogin
        /// </summary>
        public bool AutoLogin
        {
            get
            {
                return this.autoLogin;
            }
            set 
            {
                if (this.autoLogin != value)
                {
                    this.autoLogin = value;

                    if (this.autoLogin)
                        this.Rememberme = true;
                }
            }
        }

        /// <summary>
        /// OnAfterRender
        /// </summary>
        /// <param name="firstRender"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:올바르게 ValueTasks 사용", Justification = "<보류 중>")]
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            string tmpString;

            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                if (this.AuthState.IsLogin())
                    this.Navigation?.NavigateTo("/", true);

                if (this.LocalStorage != null)
                {
                    this.LoginViewModel.Email = await this.LocalStorage.GetItemAsStringAsync("Login.Email");
                    tmpString = await this.LocalStorage.GetItemAsStringAsync("Login.AutoLogin");

                    if (!this.LoginViewModel.Email.IsNullOrEmpty() && !tmpString.IsNullOrEmpty() && tmpString.ToTryBool(out bool tmpBool))
                    {
                        this.AutoLogin = tmpBool;

                        if (this.AutoLogin)
                            try
                            {
                                tmpString = await this.LocalStorage.GetItemAsStringAsync("Login.Password");

                                if (!tmpString.IsNullOrEmpty())
                                {
                                    this.LoginViewModel.Password = tmpString.AesDecryptorToBase64String(this.LoginViewModel.Email, Factory.AccessKey);

                                    if (!this.LoginViewModel.Password.IsNullOrEmpty())
                                        await this.OnLoginClickAsync();
                                    else
                                    {
                                        this.isBusy = false;
                                        this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                    }
                                }
                                else
                                {
                                    this.isBusy = false;
                                    this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                }

                            }
                            catch (Exception)
                            {
                                this.isBusy = false;
                                this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                            }
                        else
                        {
                            this.isBusy = false;
                            if (!this.LoginViewModel.Email.IsNullOrEmpty())
                                this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                            else
                                this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                        }
                    }
                    else
                    {
                        this.isBusy = false;
                        if (!this.LoginViewModel.Email.IsNullOrEmpty())
                            this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                        else
                            this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                    }
                }
                else
                {
                    this.isBusy = false;
                    this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                }

                this.StateHasChanged();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:올바르게 ValueTasks 사용", Justification = "<보류 중>")]
        private async Task<bool> OnLoginClickAsync()
        {
            try
            {
                this.LoginViewModel.IsBusy = true;

                UserInfo userInfo;

                if (this.LoginViewModel.Email != null && this.LoginViewModel.Password != null)
                {
                    if (this.Rememberme)
                        this.LocalStorage?.SetItemAsStringAsync("Login.Email", this.LoginViewModel.Email);
                    else
                        this.LocalStorage?.RemoveItemAsync("Login.Email");

                    if (this.AuthStateProvider != null)
                        this.AuthStateProvider.AuthenticationStateChanged += AuthStateProvider_AuthenticationStateChanged;

                    userInfo = await this.LoginServiceRequestAsync(this.AuthStateProvider, this.LoginViewModel.Email, this.LoginViewModel.Password);

                    if (userInfo.Status == Status.OK)
                        return true;
                    else
                    {
                        if (this.AutoLogin)
                            this.isBusy = false;

                        if (userInfo.Message != null)
                        {
                            this.ModalShow("Login", userInfo.Message, new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunctionAsync));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ModalShow("Login", $"{ex}", new() { { "Ok", Btn.Warning } }, null);
            }
            finally
            {
                this.LoginViewModel.IsBusy = false;
            }

            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:올바르게 ValueTasks 사용", Justification = "<보류 중>")]
        private void AuthStateProvider_AuthenticationStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
        {
            task.ContinueWith(t =>
            {
                if (this.LoginViewModel.Email != null && this.LoginViewModel.Password != null)
                    if (t.IsCompleted)
                    {
                        this.LocalStorage?.SetItemAsStringAsync("Login.AutoLogin", this.AutoLogin.ToString());

                        if (this.AutoLogin)
                            this.LocalStorage?.SetItemAsStringAsync("Login.Password", this.LoginViewModel.Password.AesEncryptToBase64String(this.LoginViewModel.Email, Factory.AccessKey));
                        else
                            this.LocalStorage?.RemoveItemAsync("Login.Password");

                        this.LoginViewModel.Password = string.Empty;

                        Factory.ViewModelClear();
                        this.Navigation?.NavigateTo("/", true);
                    }
            });
        }

        private async Task OnClickFunctionAsync(string action)
        {
            await Task.Delay(100);
#pragma warning disable CA2012 // 올바르게 ValueTasks 사용
            this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
#pragma warning restore CA2012 // 올바르게 ValueTasks 사용
        }

        private async void HandleValidSubmitAsync(EditContext context)
        {
            if (!this.AuthState.IsLogin())
                await this.OnLoginClickAsync();
        }

        private void OnPasswordResetClick()
        {
            this.OnAction(this, new MetaFrmEventArgs { Action = "PasswordReset" });
        }
        private void OnRegisterClick()
        {
            this.OnAction(this, new MetaFrmEventArgs { Action = "Register" });
        }
        
        private void HandleInputChange(ChangeEventArgs args)
        {
            if (args.Value != null && args.Value is string value)
                this.LoginViewModel.Email = value;
        }
        
        private void EmailKeydown(KeyboardEventArgs args)
        {
            if (args.Key == "Enter" && !this.LoginViewModel.Email.IsNullOrEmpty())
            {
#pragma warning disable CA2012 // 올바르게 ValueTasks 사용
                this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
#pragma warning restore CA2012 // 올바르게 ValueTasks 사용
            }
        }
    }
}