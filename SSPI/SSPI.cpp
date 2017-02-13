#pragma once
#include "stdafx.h"

using namespace System;
using namespace System::Collections::Specialized;

namespace SSPI
{
    public __gc class SSPIClientException : public ApplicationException
    {
    public:
        SSPIClientException(String *message, int errorCode)
        {
            this->message = message; 
            this->errorCode = errorCode;
        }
    private:
        String *message;
        int errorCode;
    };
    public __gc class SSPIClientCredential : public IDisposable
    {
    public:
        __value enum Package {Negotiate, Kerberos, NTLM};                 
        SSPIClientCredential(Package package) 
        {
            this->disposed = false;
            this->credentialHandle = __nogc new CredHandle;
            this->credentialHandle->dwLower = 0; 
            this->credentialHandle->dwUpper = 0;
            this->securityPackage = package;

            TCHAR *pszPackageName = NULL;
            if ((package == SSPIClientCredential::Package::NTLM)) {
                SSPIClientException *ex = new SSPIClientException("Only supported for Negotiate", 
                                                                  SEC_E_INTERNAL_ERROR);
                throw ex;
            } 
            else if(package == SSPIClientCredential::Package::Kerberos)
            {
                pszPackageName = __TEXT("Kerberos");
            }
            else if(package == SSPIClientCredential::Package::Negotiate)
            {
                pszPackageName = __TEXT("Negotiate");    
            }          
            
            ULONG fCredentialUse = 0;
            fCredentialUse = SECPKG_CRED_OUTBOUND;

            TimeStamp tsExpiry = { 0, 0 };			
            SECURITY_STATUS sResult = AcquireCredentialsHandle(NULL,pszPackageName,fCredentialUse,
                                                               NULL,NULL,NULL,NULL,this->credentialHandle,
                                                               &tsExpiry);

            if (sResult != SEC_E_OK)
            {
                SSPIClientException *ex = new SSPIClientException(S"AcquireCredentialsHandle failed", sResult);
                throw ex;
            }
        }
        ~SSPIClientCredential()
        {
            Dispose(false);
        }
        void Dispose() 
        {
            Dispose(true);
            GC::SuppressFinalize(this);
        }

    protected:
        virtual void Dispose(bool disposing){
            if (!this->disposed) 
            {
                if (disposing) {}
                SECURITY_STATUS sResult = FreeCredentialsHandle(
                    this->credentialHandle
                );
                delete this->credentialHandle;
                this->credentialHandle = NULL;
                if (sResult != SEC_E_OK)
                {
                    SSPIClientException *ex = new SSPIClientException(S"FreeCredentialsHandle failed", sResult);
                    throw ex;
                }
            }
            this->disposed = true;
        }
    public:
        __property Package get_SecurityPackage()
        {
            if(this->disposed)
                throw new ObjectDisposedException(this->GetType()->Name);
            return this->securityPackage;
        };
        __property IntPtr get_CredentialHandle()
        {
            if(this->disposed)
                throw new ObjectDisposedException(this->GetType()->Name);
            return IntPtr(this->credentialHandle);
        };
    private:
        CredHandle __nogc* credentialHandle;
        Package securityPackage;
        bool disposed;
    };

    public __gc __abstract class SSPIContext : public IDisposable
    {
    public:
        SSPIContext(SSPIClientCredential *credential)
        {
            this->disposed = false;
            this->token = NULL;
            this->continueProcessing = true;
            this->contextAttributes = 0;
            this->contextHandle = __nogc new CtxtHandle;
            this->contextHandle->dwLower = 0; 
            this->contextHandle->dwUpper = 0;
            this->credential = credential;
        }

        ~SSPIContext()
        {
            Dispose(false);
        }
        void Dispose() 
        {
            Dispose(true);
            GC::SuppressFinalize(this);
        }
    protected:
            virtual void Dispose(bool disposing) {

            if (!this->disposed) 
            {
                if (disposing) 
                {
                    this->credential->Dispose();
                    this->credential = NULL;
                }
                SECURITY_STATUS sResult = DeleteSecurityContext(
                this->contextHandle
                );
                delete this->contextHandle;
                this->contextHandle = NULL;
                if (sResult != SEC_E_OK)
                {
                    SSPIClientException *ex = new SSPIClientException(S"DeleteSecurityContext failed", sResult);
                    throw ex;
                }
            }
            this->disposed = true;
        }
    protected:
        Byte SecBufferToByteArray(SecBuffer &outSecBuff) []
        {
            if(this->disposed)
                throw new ObjectDisposedException(this->GetType()->Name);
            Byte outBuff[] = NULL;
            if (outSecBuff.cbBuffer > 0)            {
                outBuff = new Byte[outSecBuff.cbBuffer];
                Byte __pin *pOutBuff = &outBuff[0];
                for (unsigned long i = 0;(i < outSecBuff.cbBuffer);i++)
                    pOutBuff[i] = *((BYTE *)outSecBuff.pvBuffer + i);
                pOutBuff = NULL;
            }
            return outBuff;
        }
    public:
        __property Byte get_Token() []
        {
            if(this->disposed)
                throw new ObjectDisposedException(this->GetType()->Name);


            return this->token;
        };
    protected:        
        SSPI::SSPIClientCredential *credential;
        CtxtHandle __nogc* contextHandle;
        Byte token[];
        Boolean continueProcessing;
        ULONG contextAttributes;
    private:
        bool disposed;
    };
    
    // Client SSPIContext
    public __gc class SSPIClientContext : public SSPIContext
    {
    public:        
        [Flags]
        __value enum ContextAttributeFlags
        {
            None = 0,
            Delegate = 1,
            Identify = 2,
            MutualAuthentication = 4
        };
        SSPIClientContext(SSPIClientCredential *credential, String *serverPrincipal, ContextAttributeFlags contextAttributeFlags) : SSPIContext(credential)
        {
            this->disposed = false;
            this->serverPrincipalName = serverPrincipal;
            SecBufferDesc outBuffDesc;
            SecBuffer outSecBuff;
            BYTE outBuff[12288];
            outBuffDesc.ulVersion = SECBUFFER_VERSION;
            outBuffDesc.cBuffers = 1;
            outBuffDesc.pBuffers = &outSecBuff;
            outSecBuff.cbBuffer = 12288;
            outSecBuff.BufferType = SECBUFFER_TOKEN;
            outSecBuff.pvBuffer = outBuff;
            ULONG reqContextAttributes = ISC_REQ_CONFIDENTIALITY | \
                                         ISC_REQ_REPLAY_DETECT | \
                                         ISC_REQ_SEQUENCE_DETECT | \
                                         ISC_REQ_CONNECTION;
            TimeStamp tsLifeSpan = { 0, 0 };
            if (contextAttributeFlags & ContextAttributeFlags::Delegate)
                reqContextAttributes = reqContextAttributes | ISC_REQ_DELEGATE | ISC_REQ_MUTUAL_AUTH;
            if (contextAttributeFlags & ContextAttributeFlags::MutualAuthentication)
                reqContextAttributes = reqContextAttributes | ISC_REQ_MUTUAL_AUTH;
            if (contextAttributeFlags & ContextAttributeFlags::Identify)
                reqContextAttributes = reqContextAttributes | ISC_REQ_IDENTIFY;            
            CredHandle *phCredential = (CredHandle *)credential->CredentialHandle.ToPointer();
            ULONG __pin *pulContextAttributes = &this->contextAttributes;
            const wchar_t __pin* pwszServerPrincipalName = NULL;            
            pwszServerPrincipalName = PtrToStringChars(serverPrincipalName); 
            SECURITY_STATUS sResult = InitializeSecurityContext(phCredential,NULL,(SEC_CHAR*)pwszServerPrincipalName,
                                                                reqContextAttributes,0,SECURITY_NATIVE_DREP,                
                                                                NULL,0,this->contextHandle,&outBuffDesc,pulContextAttributes,        
                                                                &tsLifeSpan);
            phCredential = NULL;
            pulContextAttributes = NULL;
            pwszServerPrincipalName = NULL;
            if (sResult == SEC_E_OK)
                continueProcessing = false;
            else if (sResult == SEC_I_CONTINUE_NEEDED)
                continueProcessing = true;
            else
            {
                SSPIClientException *ex = new SSPIClientException(S"InitializeSecurityContext failed.", sResult);
                throw ex;
            }
            this->token = SecBufferToByteArray(outSecBuff);
        }
    protected:
        virtual void Dispose(bool disposing)
        {
            if(!this->disposed)
            {
                try
                {
                    if(disposing){}
                    this->disposed = true;
                }
                __finally
                {
                    __super::Dispose(disposing);
                }
            }
        }
    public:        
        void Initialize(Byte inToken[])
        {
            if(this->disposed)
                throw new ObjectDisposedException(this->GetType()->Name);
            SecBufferDesc inBuffDesc;
            SecBuffer inSecBuff;
            Byte __pin *pInToken = &inToken[0];
            inBuffDesc.ulVersion = SECBUFFER_VERSION;
            inBuffDesc.cBuffers = 1;
            inBuffDesc.pBuffers = &inSecBuff;
            inSecBuff.cbBuffer = inToken->Length;
            inSecBuff.BufferType = SECBUFFER_TOKEN;
            inSecBuff.pvBuffer = pInToken;            
            SecBufferDesc outBuffDesc;
            SecBuffer outSecBuff;
            BYTE outBuff[12288];
            outBuffDesc.ulVersion = SECBUFFER_VERSION;
            outBuffDesc.cBuffers = 1;
            outBuffDesc.pBuffers = &outSecBuff;
            outSecBuff.cbBuffer = 12288;
            outSecBuff.BufferType = SECBUFFER_TOKEN;
            outSecBuff.pvBuffer = outBuff;            
            ULONG reqContextAttributes = ISC_REQ_CONFIDENTIALITY | \
                                         ISC_REQ_REPLAY_DETECT | \
                                         ISC_REQ_SEQUENCE_DETECT | \
                                         ISC_REQ_CONNECTION;
            TimeStamp tsLifeSpan = { 0, 0 };
            CredHandle *phCredential = (CredHandle *)credential->CredentialHandle.ToPointer();
            ULONG __pin *pulContextAttributes = &this->contextAttributes;
            const wchar_t __pin* pwszServerPrincipalName = NULL;             
            pwszServerPrincipalName = PtrToStringChars(serverPrincipalName); 
            SECURITY_STATUS sResult = InitializeSecurityContext(phCredential, this->contextHandle,(SEC_CHAR*)pwszServerPrincipalName,                
                                                                reqContextAttributes,0,SECURITY_NATIVE_DREP,                                
                                                                &inBuffDesc,0,this->contextHandle,&outBuffDesc,                                        
                                                                pulContextAttributes,&tsLifeSpan                                            
                                                                );
            phCredential = NULL;
            pInToken = NULL;
            pulContextAttributes = NULL;
            pwszServerPrincipalName = NULL;
            if (sResult == SEC_E_OK)
                continueProcessing = false;
            else if (sResult == SEC_I_CONTINUE_NEEDED)
                continueProcessing = true;
            else
            {
                SSPIClientException *ex = new SSPIClientException(S"InitializeSecurityContext failed", sResult);
                throw ex;
            }
            this->token = SecBufferToByteArray(outSecBuff);
        }

    private:        
        String *serverPrincipalName;
        bool disposed;
    };
}

