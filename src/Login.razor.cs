using MetaFrm.Api.Models;
using MetaFrm.Control;
using MetaFrm.Razor.ViewModels;
using MetaFrm.Service;
using MetaFrm.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace MetaFrm.Razor
{
    /// <summary>
    /// Login
    /// </summary>
    public partial class Login : IDisposable
    {
        private LoginViewModel LoginViewModel { get; set; } = new(null);

        /// <summary>
        /// EditContext
        /// </summary>
        [CascadingParameter]
        private EditContext? EditContext { get; set; }

        private bool IsLoadAutoFocus;

        private bool rememberme = true;
        /// <summary>
        /// Rememberme
        /// </summary>
        private bool Rememberme
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
        private bool AutoLogin
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
        /// OnInitialized
        /// </summary>
        protected override void OnInitialized()
        {
            this.LoginViewModel = this.CreateViewModel<LoginViewModel>();

            this.EditContext ??= new EditContext(this.LoginViewModel);

            try
            {
                this.IsLoadAutoFocus = this.GetAttributeBool(nameof(this.IsLoadAutoFocus));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// OnAfterRenderAsync
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            string tmpString;

            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                if (this.AuthState.IsLogin())
                    this.Navigation?.NavigateTo("/", true);

                if (this.AuthStateProvider != null)
                {
                    this.AuthStateProvider.AuthenticationStateChanged -= AuthStateProvider_AuthenticationStateChanged;
                    this.AuthStateProvider.AuthenticationStateChanged += AuthStateProvider_AuthenticationStateChanged;
                }

                if (!this.LoginViewModel.IsBusy)
                    try
                    {
                        this.LoginViewModel.IsBusy = true;

                        if (this.LocalStorage != null)
                        {
                            this.LoginViewModel.Email = await this.LocalStorage.GetItemAsStringAsync("Login.Email");
                            tmpString = await this.LocalStorage.GetItemAsStringAsync("Login.AutoLogin") ?? "";

                            if (!string.IsNullOrEmpty(this.LoginViewModel.Email) && !string.IsNullOrEmpty(tmpString) && tmpString.ToTryBool(out bool tmpBool))
                            {
                                this.AutoLogin = tmpBool;

                                if (this.AutoLogin)
                                    try
                                    {
                                        tmpString = await this.LocalStorage.GetItemAsStringAsync("Login.Password") ?? "";

                                        if (!string.IsNullOrEmpty(tmpString))
                                        {
                                            this.LoginViewModel.Password = tmpString.AesDecryptorToBase64String(this.LoginViewModel.Email ?? "", Factory.AccessKey);

                                            if (!string.IsNullOrEmpty(this.LoginViewModel.Password))
                                                await this.OnLoginClickAsync();
                                            else
                                            {
                                                if (this.IsLoadAutoFocus)
                                                {
                                                    ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (this.IsLoadAutoFocus)
                                            {
                                                ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                            }
                                        }

                                    }
                                    catch (Exception)
                                    {
                                        if (this.IsLoadAutoFocus)
                                        {
                                            ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                        }
                                    }
                                else
                                {
                                    if (this.IsLoadAutoFocus)
                                        if (!string.IsNullOrEmpty(this.LoginViewModel.Email))
                                        {
                                            ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                        }
                                        else
                                        {
                                            ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                                        }
                                }
                            }
                            else
                            {
                                if (this.IsLoadAutoFocus)
                                    if (!string.IsNullOrEmpty(this.LoginViewModel.Email))
                                    {
                                        ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                                    }
                                    else
                                    {
                                        ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                                    }
                            }
                        }
                        else
                        {
                            if (this.IsLoadAutoFocus)
                            {
                                ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                            }
                        }
                    }
                    finally
                    {
                        this.LoginViewModel.IsBusy = false;
                    }

                this.StateHasChanged();
            }
        }

        private async Task<bool> OnLoginClickAsync()
        {
            try
            {
                this.LoginViewModel.IsBusy = true;

                UserInfo userInfo;

                if (this.LoginViewModel.Email != null && this.LoginViewModel.Password != null)
                {
                    if (this.Rememberme)
                    {
                        ValueTask? _ = this.LocalStorage?.SetItemAsStringAsync("Login.Email", this.LoginViewModel.Email);
                    }
                    else
                    {
                        ValueTask? _ = this.LocalStorage?.RemoveItemAsync("Login.Email");
                    }

                    userInfo = await this.LoginServiceRequestAsync(this.AuthStateProvider, this.LoginViewModel.Email, this.LoginViewModel.Password);

                    if (userInfo.Status == Status.OK)
                        return true;
                    else
                    {
                        if (userInfo.Message != null)
                        {
                            this.ModalShow("로그인", userInfo.Message, new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunctionAsync));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.ModalShow("로그인", $"{ex}", new() { { "Ok", Btn.Warning } }, null);
            }
            finally
            {
                this.LoginViewModel.IsBusy = false;
            }

            return false;
        }

        private void AuthStateProvider_AuthenticationStateChanged(Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> task)
        {
            task.ContinueWith(t =>
            {
                if (this.LoginViewModel.Email != null && this.LoginViewModel.Password != null)
                    if (t.IsCompleted)
                    {
                        if (this.LoginViewModel.IsBusy) return;

                        try
                        {
                            this.LoginViewModel.IsBusy = true;

                            ValueTask? _ = this.LocalStorage?.SetItemAsStringAsync("Login.AutoLogin", this.AutoLogin.ToString());

                            if (this.AutoLogin)
                            {
                                ValueTask? __ = this.LocalStorage?.SetItemAsStringAsync("Login.Password", this.LoginViewModel.Password.AesEncryptToBase64String(this.LoginViewModel.Email, Factory.AccessKey));
                            }
                            else
                            {
                                ValueTask? __ = this.LocalStorage?.RemoveItemAsync("Login.Password");
                            }

                            this.LoginViewModel.Password = string.Empty;

                            Factory.ViewModelClear();

                        }
                        catch (Exception ex)
                        {
                            Factory.Logger.Error(ex, "Error while AuthStateProvider_AuthenticationStateChanged");
                        }
                        finally
                        {
                            this.Navigation?.NavigateTo("/", true);

                            this.LoginViewModel.IsBusy = false;
                        }
                    }
            });
        }

        private async Task OnClickFunctionAsync(string action)
        {
            await Task.Delay(100);

            if (this.IsLoadAutoFocus)
            {
                ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
            }
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
            if (args.Key == "Enter" && !string.IsNullOrEmpty(this.LoginViewModel.Email))
            {
                ValueTask? _ = this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
            }

            if (this.EditContext != null)
                this.EditContext?.Validate();
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.AuthStateProvider != null)
                    this.AuthStateProvider.AuthenticationStateChanged -= AuthStateProvider_AuthenticationStateChanged;
            }
        }
    }
}