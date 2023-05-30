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

        /// <summary>
        /// Rememberme
        /// </summary>
        public bool Rememberme { get; set; } = true;

        /// <summary>
        /// OnAfterRender
        /// </summary>
        /// <param name="firstRender"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:올바르게 ValueTasks 사용", Justification = "<보류 중>")]
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                if (this.IsLogin())
                    this.Navigation?.NavigateTo("/", true);

                if (this.LocalStorage != null)
                {
                    this.LoginViewModel.Email = await this.LocalStorage.GetItemAsStringAsync("Login.Email");

                    if (!this.LoginViewModel.Email.IsNullOrEmpty())
                        this.JSRuntime?.InvokeVoidAsync("ElementFocus", "password");
                    else
                        this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");
                }
                else
                    this.JSRuntime?.InvokeVoidAsync("ElementFocus", "email");

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

                    userInfo = await this.LoginServiceRequestAsync(this.LoginViewModel.Email, this.LoginViewModel.Password);

                    if (userInfo.Status == Status.OK)
                    {
                        this.LoginViewModel.Password = string.Empty;

                        if (AuthStateProvider != null)
                        {
                            AuthenticationStateProvider authenticationStateProvider = (AuthenticationStateProvider)AuthStateProvider;

                            await authenticationStateProvider.SetSessionTokenAsync(userInfo.Token);
                            authenticationStateProvider.Notify();
                        }

                        Factory.ViewModelClear();

                        this.Navigation?.NavigateTo("/", true);
                        return true;
                    }
                    else
                    {
                        if (userInfo.Message != null)
                        {
                            this.ModalShow("Login", userInfo.Message, new() { { "Ok", Btn.Warning } }, EventCallback.Factory.Create<string>(this, OnClickFunctionAsync));
                        }
                    }
                }
            }
            finally
            {
                this.LoginViewModel.IsBusy = false;
            }

            return false;
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
            if (!this.IsLogin())
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