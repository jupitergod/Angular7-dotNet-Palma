using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.MultiTenancy;
using Abp.UI;
using Abp.Web.Models;
using Flurl.Http;
using JetBrains.Annotations;
using PALMASoft.ApiClient.Models;
using PALMASoft.Authorization.Accounts.Dto;

namespace PALMASoft.ApiClient
{
    public class AccessTokenManager : IAccessTokenManager, ISingletonDependency
    {
        private const string LoginUrlSegment = "api/TokenAuth/Authenticate";
        private const string RefreshTokenUrlSegment = "api/TokenAuth/RefreshToken";

        private readonly AbpAuthenticateModel _authenticateModel;
        private readonly IApplicationContext _applicationContext;

        public DateTime AccessTokenRetrieveTime { get; set; }

        [CanBeNull]
        public AbpAuthenticateResultModel AuthenticateResult { get; set; }

        public bool IsUserLoggedIn => AuthenticateResult?.AccessToken != null;

        public bool IsRefreshTokenExpired => AuthenticateResult == null || DateTime.Now >= AuthenticateResult.RefreshTokenExpireDate;

        public AccessTokenManager(
            IApplicationContext applicationContext,
            AbpAuthenticateModel authenticateModel)
        {
            _applicationContext = applicationContext;
            _authenticateModel = authenticateModel;
        }

        public string GetAccessToken()
        {
            if (AuthenticateResult == null)
            {
                throw new ApplicationException("You have to authenticate first!");
            }

            return AuthenticateResult.AccessToken;
        }

        public async Task<AbpAuthenticateResultModel> LoginAsync()
        {
            EnsureUserNameAndPasswordProvided();

            using (var client = CreateApiClient())
            {
                if (_applicationContext.CurrentTenant != null)
                {
                    client.WithHeader(MultiTenancyConsts.TenantIdResolveKey, _applicationContext.CurrentTenant.TenantId);
                }

                var response = await client
                    .Request(LoginUrlSegment)
                    .PostJsonAsync(_authenticateModel)
                    .ReceiveJson<AjaxResponse<AbpAuthenticateResultModel>>();

                if (!response.Success || response.Result == null)
                {
                    AuthenticateResult = null;
                    throw new UserFriendlyException(response.Error.Message + ": " + response.Error.Details);
                }

                AuthenticateResult = response.Result;
                AuthenticateResult.RefreshTokenExpireDate = DateTime.Now.Add(AppConsts.RefreshTokenExpiration);

                return AuthenticateResult;
            }
        }

        public async Task<string> RefreshTokenAsync()
        {
            if (AuthenticateResult == null ||
                string.IsNullOrWhiteSpace(AuthenticateResult.RefreshToken))
            {
                throw new ApplicationException("No refresh token!");
            }

            using (var client = CreateApiClient())
            {
                var response = await client.Request(RefreshTokenUrlSegment)
                    .PostUrlEncodedAsync(new { refreshToken = AuthenticateResult.RefreshToken })
                    .ReceiveJson<AjaxResponse<RefreshTokenResult>>();

                if (!response.Success)
                {
                    AuthenticateResult = null;
                    throw new UserFriendlyException(response.Error.Message + ": " + response.Error.Details);
                }

                AuthenticateResult.AccessToken = response.Result.AccessToken;
                AccessTokenRetrieveTime = DateTime.Now;

                return response.Result.AccessToken;
            }
        }

        public void Logout()
        {
            AuthenticateResult = null;
        }

        private void EnsureUserNameAndPasswordProvided()
        {
            if (_authenticateModel.UserNameOrEmailAddress == null ||
                _authenticateModel.Password == null)
            {
                throw new Exception("Username or password fields cannot be empty!");
            }
        }

        private static IFlurlClient CreateApiClient()
        {
            IFlurlClient client = new FlurlClient(ApiUrlConfig.BaseUrl);
            client.WithHeader("Accept", new MediaTypeWithQualityHeaderValue("application/json"));
            client.WithHeader("User-Agent", AbpApiClient.UserAgent);
            return client;
        }
    }
}