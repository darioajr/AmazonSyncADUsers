using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.S3;
using Amazon.S3.Model;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Mail;

namespace AmazonSyncADUsers
{
    class AmazonS3Helper
    {
        private static string AdministratorEmail = ConfigurationManager.AppSettings["AdministratorEmail"];
        private static string MAILMessage = ConfigurationManager.AppSettings["MAILMessage"];
        private static string MAILSubject = ConfigurationManager.AppSettings["MAILSubject"];
        private static string MAILAttach = ConfigurationManager.AppSettings["MAILAttach"];

        private static string AMAZONSecurityGroup = ConfigurationManager.AppSettings["AMAZONSecurityGroup"];
        private static string AMAZONBucket = ConfigurationManager.AppSettings["AMAZONBucket"];
        private static string AMAZONPersonalFolder = ConfigurationManager.AppSettings["AMAZONPersonalFolder"];
       
        public static bool CreateUser(string userName, string groupName)
        {
            try
            {
                //TODO: Receber usuario e email
                //é feio mais eu fiz isso aqui, pra evitar esforço extra, futuramente será corrigido (será?)
                var userAD = ActiveDirectoryHelper.GetUserPrincipal(userName);
                var userEmail = userAD.EmailAddress;
                var userDisplayName = userAD.DisplayName;
                var attachment = AppDomain.CurrentDomain.BaseDirectory + MAILAttach;
                var email = string.IsNullOrEmpty(userEmail) ? 
                            AdministratorEmail : 
                            (string.IsNullOrEmpty(AdministratorEmail) ? 
                            userEmail : 
                            string.Format("{0}, {1}", userEmail, AdministratorEmail));

                var iamClient = AWSClientFactory.CreateAmazonIdentityManagementServiceClient();

                // Create the IAM user
                var User = iamClient.CreateUser(new CreateUserRequest
                {
                    UserName = userName
                }).User;

                iamClient.AddUserToGroup(new AddUserToGroupRequest
                {
                    UserName = userName,
                    GroupName = groupName
                });

                // Create an access key for the IAM user
                var accessKey = iamClient.CreateAccessKey(new CreateAccessKeyRequest
                {
                    UserName = userName
                }).AccessKey;

                var message = string.Format(MAILMessage, userDisplayName, userName, accessKey.AccessKeyId, accessKey.SecretAccessKey);
   
                //envia email para o administrador e usuário
                SendMail.EmailTo(email, MAILSubject, message, attachment);

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool RemoveUserFromGroup(string userName, string groupName)
        {
            try
            {
                var iamClient = AWSClientFactory.CreateAmazonIdentityManagementServiceClient();

                iamClient.RemoveUserFromGroup(new RemoveUserFromGroupRequest
                {
                    UserName = userName,
                    GroupName = groupName
                });

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool AddUserOnGroup(string userName, string groupName)
        {
            try
            {
                var iamClient = AWSClientFactory.CreateAmazonIdentityManagementServiceClient();

                iamClient.AddUserToGroup(new AddUserToGroupRequest
                {
                    UserName = userName,
                    GroupName = groupName
                });

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool RemoveUsersNotIn(List<string> users)
        {
            try
            {
                var iamClient = AWSClientFactory.CreateAmazonIdentityManagementServiceClient();
                var usersIn = iamClient.ListUsers(new ListUsersRequest());
                foreach(var user in usersIn.Users)
                {
                    if (!users.Contains(user.UserName, StringComparer.OrdinalIgnoreCase))
                    {
                        RemoveUserFromGroup(user.UserName, AMAZONSecurityGroup);


                        var listAccessKeysReponse = iamClient.ListAccessKeys(new ListAccessKeysRequest
                        {
                            UserName = user.UserName
                        });

                        var deleteAccessKeyReponse = iamClient.DeleteAccessKey(new DeleteAccessKeyRequest
                        {
                            UserName = user.UserName,
                            AccessKeyId = listAccessKeysReponse.AccessKeyMetadata[0].AccessKeyId
                        });


                        iamClient.DeleteUser(new DeleteUserRequest
                        {
                            UserName = user.UserName
                        });
                    }
                }

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool CreateUsers(List<string> users)
        {
            try
            {
                var iamClient = AWSClientFactory.CreateAmazonIdentityManagementServiceClient();
                var usersIn = iamClient.ListUsers(new ListUsersRequest());

                foreach (var user in users)
                {
                    if(!usersIn.Users.Exists(o => o.UserName == user))
                    {
                        CreateUser(user, AMAZONSecurityGroup);
                        if (!DoesFolderExist(string.Format(AMAZONPersonalFolder + "{0}/", user), AMAZONBucket))
                        {
                            try
                            {
                                CreateUserFolder(AMAZONBucket, string.Format(AMAZONPersonalFolder + "{0}/", user));
                            }
                            catch {};
                        }
                    }
                }

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool CreateUserFolder(string bucket, string folderName)
        {
            try
            {
                // Create an S3 client with the IAM user's access key
                var s3Client = AWSClientFactory.CreateAmazonS3Client();

                var response = s3Client.PutObject(new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = folderName,
                    ContentBody = string.Empty
                });

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static bool DoesFolderExist(string key, string bucketName)
        {
            try
            {
                // Create an S3 client with the IAM user's access key
                var s3Client = AWSClientFactory.CreateAmazonS3Client();

                var response = s3Client.GetObjectMetadata(new GetObjectMetadataRequest
                {
                    BucketName = bucketName,
                    Key = key
                });

                return true;
            }
            catch (Amazon.S3.AmazonS3Exception ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return false;

                throw;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
