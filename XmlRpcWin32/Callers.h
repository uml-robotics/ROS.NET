#ifndef __Callers_h__
#define __Callers_h__

#include "XmlRpcServerMethodWrapper.h"
#include "XmlRpc.h"        // needed for XMLRPC_API
using namespace XmlRpc;
#ifdef __cplusplus
extern "C" {
#endif
    //general    
    typedef void (*EricRulz)(const char *c);
    extern XMLRPC_API void SetStringOutFunc(EricRulz fptr);
    extern XMLRPC_API void SetLogLevel(int level);
    extern XMLRPC_API void XmlRpcGiblets_Free(void *instance);
    //XmlRpcClient
    extern XMLRPC_API XmlRpcClient* XmlRpcClient_Create(const char *host, int port, const char *uri);
    extern XMLRPC_API void XmlRpcClient_Close(XmlRpcClient* instance);
    extern XMLRPC_API unsigned char XmlRpcClient_TestConnection(XmlRpcClient* instance);
    extern XMLRPC_API unsigned char XmlRpcClient_Execute(XmlRpcClient* instance, const char* method, XmlRpcValue *parameterss, XmlRpcValue *result);
    extern XMLRPC_API unsigned char XmlRpcClient_ExecuteNonBlock(XmlRpcClient* instance, const char* method, XmlRpcValue *parameterss);
    extern XMLRPC_API unsigned char XmlRpcClient_ExecuteCheckDone(XmlRpcClient* instance, XmlRpcValue *result);
    extern XMLRPC_API unsigned XmlRpcClient_HandleEvent(XmlRpcClient* instance, unsigned eventType);
    extern XMLRPC_API unsigned char XmlRpcClient_IsFault(XmlRpcClient* instance);
    extern XMLRPC_API void XmlRpcClient_ClearFault(XmlRpcClient* instance);
    extern XMLRPC_API const char* XmlRpcClient_GetHost(XmlRpcClient* instance);
    extern XMLRPC_API const char* XmlRpcClient_GetUri(XmlRpcClient* instance);
    extern XMLRPC_API int XmlRpcClient_GetPort(XmlRpcClient* instance);
    extern XMLRPC_API const char* XmlRpcClient_GetRequest(XmlRpcClient* instance);
    extern XMLRPC_API const char* XmlRpcClient_GetHeader(XmlRpcClient* instance);
    extern XMLRPC_API const char* XmlRpcClient_GetResponse(XmlRpcClient* instance);
    extern XMLRPC_API int XmlRpcClient_GetSendAttempts(XmlRpcClient* instance);
    extern XMLRPC_API int XmlRpcClient_GetBytesWritten(XmlRpcClient* instance);
    extern XMLRPC_API unsigned char XmlRpcClient_GetExecuting(XmlRpcClient* instance);
    extern XMLRPC_API unsigned char XmlRpcClient_GetEOF(XmlRpcClient* instance, XmlRpcValue *result);
    extern XMLRPC_API int XmlRpcClient_GetContentLength(XmlRpcClient* instance);
    extern XMLRPC_API XmlRpcDispatch* XmlRpcClient_GetXmlRpcDispatch(XmlRpcClient* instance);

    //XmlRpcValue    
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create1();
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create2(bool value);
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create3(int value);
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create4(double value);
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create5(const char* value);
    extern XMLRPC_API XmlRpcValue *XmlRpcValue_Create6(XmlRpcValue *rhs);
    extern XMLRPC_API void XmlRpcValue_Clear(XmlRpcValue* instance);
    extern XMLRPC_API unsigned char XmlRpcValue_Valid(XmlRpcValue* instance);
    extern XMLRPC_API void XmlRpcValue_SetType(XmlRpcValue* instance, int t);
    extern XMLRPC_API int XmlRpcValue_Type(XmlRpcValue* instance);
    extern XMLRPC_API int XmlRpcValue_Size(XmlRpcValue* instance);
    extern XMLRPC_API void XmlRpcValue_SetSize(XmlRpcValue* instance, int size);
    extern XMLRPC_API unsigned char XmlRpcValue_HasMember(XmlRpcValue* instance, const char* name);
    extern XMLRPC_API XmlRpcValue* XmlRpcValue_Get1(XmlRpcValue* instance, int key);
    extern XMLRPC_API XmlRpcValue* XmlRpcValue_Get2(XmlRpcValue* instance, const char* key);
    extern XMLRPC_API void XmlRpcValue_Set1(XmlRpcValue* instance, const char *value);
    extern XMLRPC_API void XmlRpcValue_Set3(XmlRpcValue* instance, XmlRpcValue *value);
    extern XMLRPC_API void XmlRpcValue_Set5(XmlRpcValue* instance, int value);
    extern XMLRPC_API void XmlRpcValue_Set7(XmlRpcValue* instance, bool value);
    extern XMLRPC_API void XmlRpcValue_Set9(XmlRpcValue* instance, double value);
    extern XMLRPC_API int XmlRpcValue_GetInt0(XmlRpcValue* instance);
    extern XMLRPC_API const char* XmlRpcValue_GetString0(XmlRpcValue* instance);
    extern XMLRPC_API unsigned char XmlRpcValue_GetBool0(XmlRpcValue* instance);
    extern XMLRPC_API double XmlRpcValue_GetDouble0(XmlRpcValue* instance);
    extern XMLRPC_API void XmlRpcValue_Dump(XmlRpcValue* instance);
    extern XMLRPC_API const char* XmlRpcValue_ToString(XmlRpcValue* instance);

    //XmlRpcDispatch
    extern XMLRPC_API XmlRpcDispatch *XmlRpcDispatch_Create();
    extern XMLRPC_API void XmlRpcDispatch_Close(XmlRpcDispatch *instance);
    extern XMLRPC_API void XmlRpcDispatch_AddSource(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned eventMask);
    extern XMLRPC_API void XmlRpcDispatch_RemoveSource(XmlRpcDispatch* instance, XmlRpcSource *source);
    extern XMLRPC_API void XmlRpcDispatch_SetSourceEvents(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned eventMask);
    extern XMLRPC_API void XmlRpcDispatch_Work(XmlRpcDispatch *instance, double msTime);
    extern XMLRPC_API void XmlRpcDispatch_Exit(XmlRpcDispatch *instance);
    extern XMLRPC_API void XmlRpcDispatch_Clear(XmlRpcDispatch *instance);

    //XmlRpcSource
    extern XMLRPC_API XmlRpcSource *XmlRpcSource_Create(int fd, bool deleteOnClose);
    extern XMLRPC_API void XmlRpcSource_Close(XmlRpcSource *instance);
    extern XMLRPC_API int XmlRpcSource_GetFd(XmlRpcSource *instance);
    extern XMLRPC_API void XmlRpcSource_SetFd(XmlRpcSource *instance, int fd);
    extern XMLRPC_API unsigned char XmlRpcSource_GetKeepOpen(XmlRpcSource *instance);
    extern XMLRPC_API void XmlRpcSource_SetKeepOpen(XmlRpcSource *instance, bool b);
    extern XMLRPC_API unsigned handleEvent(unsigned eventType);

    //XmlRpcServerMethod
    extern XMLRPC_API XmlRpcServerMethodWrapper *XmlRpcServerMethod_Create(char *name, XmlRpcServer *server);
    extern XMLRPC_API void XmlRpcServerMethod_SetFunc(XmlRpcServerMethodWrapper *instance, XmlRpcServerFUNC func);
    extern XMLRPC_API void XmlRpcServerMethod_Execute(XmlRpcServerMethodWrapper *instance, XmlRpcValue *parms, XmlRpcValue *res);
    
    //XmlRpcServer    
    extern XMLRPC_API XmlRpcServer *XmlRpcServer_Create();
    extern XMLRPC_API void XmlRpcServer_AddMethod(XmlRpcServer *instance, XmlRpcServerMethod *method);
    extern XMLRPC_API void XmlRpcServer_RemoveMethod(XmlRpcServer *instance, XmlRpcServerMethod *method);
    extern XMLRPC_API void XmlRpcServer_RemoveMethodByName(XmlRpcServer *instance, char *name);
    extern XMLRPC_API XmlRpcServerMethod *XmlRpcServer_FindMethod(XmlRpcServer *instance, char *name);
    extern XMLRPC_API unsigned char XmlRpcServer_BindAndListen(XmlRpcServer *instance, int port, int backlog);
    extern XMLRPC_API void XmlRpcServer_Work(XmlRpcServer *instance,double msTime);
    extern XMLRPC_API void XmlRpcServer_Exit(XmlRpcServer *instance);
    extern XMLRPC_API void XmlRpcServer_Shutdown(XmlRpcServer *instance);
    extern XMLRPC_API int XmlRpcServer_GetPort(XmlRpcServer *instance);
    extern XMLRPC_API XmlRpcDispatch *XmlRpcServer_GetDispatch(XmlRpcServer *instance);
#ifdef __cplusplus
}
#endif

#endif