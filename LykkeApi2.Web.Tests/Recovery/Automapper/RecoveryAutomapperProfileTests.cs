using AutoMapper;
using Core.Domain.Recovery;
using Core.Dto.Recovery;
using Lykke.Service.ClientAccountRecovery.Client.AutoRestClient.Models;
using LykkeApi2.Automapper;
using LykkeApi2.Models.Recovery;
using NUnit.Framework;

namespace Lykke.WalletApiv2.Tests.Recovery.Automapper
{
    [TestFixture]
    public class RecoveryAutomapperProfileTests
    {
        private IMapper _mapper;

        private const string Email = "test@test.com";
        private const string PartnerId = "TestPartnerId";
        private const string Ip = "1.1.1.1";
        private const string UserAgent = "TestUserAgent";
        private const string RecoveryId = "TestRecoveryId";
        private const string StateToken = "TestStateToken";
        private const string Value = "TestValue";
        private const string ClientId = "TestClientId";
        private const string PasswordHash = "TestPasswordHash";
        private const string ChallengeInfo = "TestChallengeInfo";
        private const string Pin = "TestPin";
        private const string Hint = "TestHint";

        private Challenge _challenge = Challenge.Email;
        private Action _action = Action.Skip;
        private Progress _progress = Progress.Allowed;
        private MapperConfiguration _mapperConfiguration;

        [OneTimeSetUp]
        public void Init()
        {
            _mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<RecoveryAutomapperProfile>());
            _mapper = new Mapper(_mapperConfiguration);
        }

        [Test]
        public void Configuration_IsValid()
        {
            _mapperConfiguration.AssertConfigurationIsValid();
        }
        [Test]
        public void Map_RecoveryStartRequestModelToRecoveryStartDto_IsValid()
        {
            // Arrange
            var source = new RecoveryStartRequestModel
            {
                Email = Email,
                PartnerId = PartnerId
            };

            // Act
            var dest = _mapper.Map<RecoveryStartRequestModel, RecoveryStartDto>(source,
                opt =>
                {
                    opt.Items["Ip"] = Ip;
                    opt.Items["UserAgent"] = UserAgent;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(Email, dest.Email);
                Assert.AreEqual(PartnerId, dest.PartnerId);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }

        [Test]
        public void Map_RecoveryStartDtoToNewRecoveryRequest_IsValid()
        {
            // Arrange
            var source = new RecoveryStartDto
            {
                Email = Email,
                PartnerId = PartnerId,
                Ip = Ip,
                UserAgent = UserAgent 
            };

            // Act
            var dest = _mapper.Map<RecoveryStartDto, NewRecoveryRequest>(source,
                opt =>
                {
                    opt.Items["ClientId"] = ClientId;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(ClientId, dest.ClientId);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }

        [Test]
        public void Map_RecoveryStatusResponseToRecoveryStatus_IsValid()
        {
            // Arrange
            var source = new RecoveryStatusResponse(_challenge, _progress, ChallengeInfo);

            // Act
            var dest = _mapper.Map<RecoveryStatusResponse, RecoveryStatus>(source);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(_challenge, dest.Challenge);
                Assert.AreEqual(_progress, dest.OverallProgress);
                Assert.AreEqual(ChallengeInfo, dest.ChallengeInfo);
            });
        }

        [Test]
        public void RecoveryStatusToRecoveryStatusResponseModel()
        {
            // Arrange
            var source = new RecoveryStatus
            {
                Challenge = _challenge,
                OverallProgress = _progress,
                ChallengeInfo = ChallengeInfo
            };

            // Act
            var dest = _mapper.Map<RecoveryStatus, RecoveryStatusResponseModel>(source);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(_challenge, dest.Challenge);
                Assert.AreEqual(_progress, dest.OverallProgress);
                Assert.AreEqual(ChallengeInfo, dest.ChallengeInfo);
            });
        }

        [Test]
        public void Map_RecoverySubmitChallengeDtoToChallengeRequest_IsValid()
        {
            // Arrange
            var source = new RecoverySubmitChallengeDto
            {
                Action = _action,
                StateToken = StateToken,
                Value = Value,
                UserAgent = UserAgent,
                Ip = Ip,
            };

            // Act
            var dest = _mapper.Map<RecoverySubmitChallengeDto, ChallengeRequest>(source,
                opt =>
                {
                    opt.Items["Challenge"] = _challenge;
                    opt.Items["RecoveryId"] = RecoveryId;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(RecoveryId, dest.RecoveryId);
                Assert.AreEqual(_challenge, dest.Challenge);
                Assert.AreEqual(_action, dest.Action);
                Assert.AreEqual(Value, dest.Value);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }

        [Test]
        public void Map_RecoverySubmitChallengeRequestModelToRecoverySubmitChallengeDto_IsValid()
        {
            // Arrange
            var source = new RecoverySubmitChallengeRequestModel
            {
                StateToken = StateToken,
                Action = _action,
                Value = Value
            };

            // Act
            var dest = _mapper.Map<RecoverySubmitChallengeRequestModel, RecoverySubmitChallengeDto>(source,
                opt =>
                {
                    opt.Items["Ip"] = Ip;
                    opt.Items["UserAgent"] = UserAgent;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(_action, dest.Action);
                Assert.AreEqual(Value, dest.Value);
                Assert.AreEqual(StateToken, dest.StateToken);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }

        [Test]
        public void Map_RecoveryCompleteRequestModelToRecoveryCompleteDto_IsValid()
        {
            // Arrange
            var source = new RecoveryCompleteRequestModel
            {
                PasswordHash = PasswordHash,
                StateToken = StateToken
            };

            // Act
            var dest = _mapper.Map<RecoveryCompleteRequestModel, RecoveryCompleteDto>(source,
                opt =>
                {
                    opt.Items["Ip"] = Ip;
                    opt.Items["UserAgent"] = UserAgent;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(PasswordHash, dest.PasswordHash);
                Assert.AreEqual(StateToken, dest.StateToken);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }

        [Test]
        public void Map_RecoveryCompleteDtoToPasswordRequest_IsValid()
        {
            // Arrange
            var source = new RecoveryCompleteDto
            {
                PasswordHash = PasswordHash,
                Pin = Pin,
                Hint = Hint,
                Ip = Ip,
                UserAgent = UserAgent
            };

            // Act
            var dest = _mapper.Map<RecoveryCompleteDto, PasswordRequest>(source,
                opt =>
                {
                    opt.Items["RecoveryId"] = RecoveryId;
                });

            // Assert
            Assert.Multiple(() =>
            {
                Assert.AreEqual(PasswordHash, dest.PasswordHash);
                Assert.AreEqual(Pin, dest.Pin);
                Assert.AreEqual(Hint, dest.Hint);
                Assert.AreEqual(RecoveryId, dest.RecoveryId);
                Assert.AreEqual(Ip, dest.Ip);
                Assert.AreEqual(UserAgent, dest.UserAgent);
            });
        }
    }
}