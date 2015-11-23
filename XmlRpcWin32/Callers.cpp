#include <windows.h>
#include "Callers.h"
#include "EricRulz.h"
#include <errno.h>
#include <exception>

using namespace XmlRpc;

EricRulz OUTREF;

//pass log messages back to managed invoker
extern "C" XMLRPC_API void SetStringOutFunc(EricRulz fptr)
{
    OUTREF = fptr;
}

extern "C" XMLRPC_API void SetLogLevel(int level)
{
    setVerbosity(level);
}

//used to explicitly clean up shop
extern "C" XMLRPC_API void XmlRpcGiblets_Free(void *ptr) {
    delete ptr;
}

//client
extern "C" XMLRPC_API XmlRpcClient* XmlRpcClient_Create(const char *host, int port, const char *uri)
{
    try
    {
        return (new XmlRpcClient(host, port, uri));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API void XmlRpcClient_Close(XmlRpcClient* instance)
{
    try
    {
        (*instance).close();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_Execute(XmlRpcClient* instance, const char* method, XmlRpcValue *parameters, XmlRpcValue *result)
{
    try
    {
        return ((*instance).execute(method, *parameters, *result)) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_ExecuteNonBlock(XmlRpcClient* instance, const char* method, XmlRpcValue *parameters)
{
    try
    {
        return ((*instance).executeNonBlock(method, *parameters)) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_IsConnected(XmlRpcClient* instance)
{
    return (*instance).testConnection();
}

extern "C" XMLRPC_API unsigned char XmlRpcClient_ExecuteCheckDone(XmlRpcClient* instance, XmlRpcValue *result)
{
    try
    {
        return ((*instance).executeCheckDone(*result)) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API unsigned XmlRpcClient_HandleEvent(XmlRpcClient* instance, unsigned eventType)
{
    try
    {
        return ((*instance).handleEvent(eventType));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_IsFault(XmlRpcClient* instance)
{
    try
    {
        return ((*instance).isFault()) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API void XmlRpcClient_ClearFault(XmlRpcClient* instance)
{
    try
    {
        ((*instance).clearFault());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetHost(XmlRpcClient* instance)
{
    try
    {
        if (instance == NULL) return "";
        return (*instance)._host.c_str();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetUri(XmlRpcClient* instance)
{
    try
    {
        if (instance == NULL) return "";
        return (*instance)._uri.c_str();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API int XmlRpcClient_GetPort(XmlRpcClient* instance)
{
    try
    {
        return ((*instance).getPort());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return -1;
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetRequest(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._request.c_str());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetHeader(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._header.c_str());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetResponse(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._response.c_str());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API int XmlRpcClient_GetSendAttempts(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._sendAttempts);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API int XmlRpcClient_GetBytesWritten(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._bytesWritten);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_GetExecuting(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._executing) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API unsigned char XmlRpcClient_GetEOF(XmlRpcClient* instance, XmlRpcValue *result)
{
    try
    {
        return ((*instance)._eof) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API int XmlRpcClient_GetContentLength(XmlRpcClient* instance)
{
    try
    {
        return ((*instance)._contentLength);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API XmlRpcDispatch* XmlRpcClient_GetXmlRpcDispatch(XmlRpcClient* instance)
{
    try
    {
        return &((*instance)._disp);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}

//value
typedef XmlRpcValue *Val;
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create1()
{
    try
    {
        return new XmlRpcValue();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create2(bool value)
{
    try
    {
        return new XmlRpcValue(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create3(int value)
{
    try
    {
        return new XmlRpcValue(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create4(double value)
{
    try
    {
    return new XmlRpcValue(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create5(const char* value)
{
    try
    {
        return new XmlRpcValue(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}

extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create6(XmlRpcValue *rhs)
{
    try
    {
        return new XmlRpcValue(*rhs);    
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API unsigned char XmlRpcValue_Valid(XmlRpcValue* instance)
{
    try
    {
        
    return ((*instance).valid()) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API int XmlRpcValue_Type(XmlRpcValue* instance)
{
    try
    {
        return (int)((*instance).getType());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API int XmlRpcValue_Size(XmlRpcValue* instance)
{
    try
    {
        return ((*instance).size());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API void XmlRpcValue_SetSize(XmlRpcValue* instance, int size)
{
    try
    {
        (*instance).setSize(size);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API unsigned char XmlRpcValue_HasMember(XmlRpcValue* instance, const char* name)
{
    try
    {
        return ((*instance).hasMember(name)) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API XmlRpcValue* XmlRpcValue_Get1(XmlRpcValue* instance, int key)
{
    try
    {
        return &((*instance).operator[](key));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
    //return &instance->operator[](key);
}
extern "C" XMLRPC_API XmlRpcValue* XmlRpcValue_Get2(XmlRpcValue* instance, const char* key)
{
    try
    {
        return &((*instance).operator[](key));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
    //return &instance->operator[](key);
}
extern "C" XMLRPC_API void XmlRpcValue_Set1(XmlRpcValue* instance, const char *value)
{
    try
    {
        (*instance).operator=(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcValue_Set3(XmlRpcValue* instance, XmlRpcValue *value)
{
    try
    {
        (*instance).operator=(*value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcValue_Set5(XmlRpcValue* instance, int value)
{
    try
    {
        (*instance).operator=(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcValue_Set7(XmlRpcValue* instance, bool value)
{
    try
    {
        (*instance).operator=(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcValue_Set9(XmlRpcValue* instance, double value)
{
    try
    {
        (*instance).operator=(value);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API int XmlRpcValue_GetInt0(XmlRpcValue* instance)
{
    try
    {
        return (((*instance).operator int &()));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API const char* XmlRpcValue_GetString0(XmlRpcValue* instance)
{
    try
    {
        return (((*instance).operator std::string &()).c_str());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return "";
}
extern "C" XMLRPC_API unsigned char XmlRpcValue_GetBool0(XmlRpcValue* instance)
{
    try
    {
        return ((*instance).operator bool &()) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API double XmlRpcValue_GetDouble0(XmlRpcValue* instance)
{
    try
    {
        return ((*instance).operator double &());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}
extern "C" XMLRPC_API void XmlRpcValue_Dump(XmlRpcValue* instance)
{
    try
    {
        XmlRpcUtil::log(0, "XmlRpcValue Dump:\n\tptr=%d\n\ttype=%d\n\tsize=%d\n%s\n", (int)instance, (*instance)._type, (*instance).size(), ((*instance).getType() != XmlRpcValue::TypeInvalid ? (*instance).toXml().c_str() : "INVALID"));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}

extern "C" XMLRPC_API const char* XmlRpcValue_ToString(XmlRpcValue* instance)
{
  static char buf[2048];
  memset(buf, 0, 2048);
  sprintf(buf, "%s", (*instance).toXml().c_str());
    return buf;
}

//dispatch
extern "C" XMLRPC_API XmlRpcDispatch *XmlRpcDispatch_Create()
{
    try
    {
        return new XmlRpcDispatch();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API void XmlRpcDispatch_Close(XmlRpcDispatch *instance)
{
    try
    {
        (*instance).~XmlRpcDispatch();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    
}
extern "C" XMLRPC_API void XmlRpcDispatch_AddSource(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned eventMask)
{
    try
    {
        (*instance).addSource(source, (unsigned)eventMask);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcDispatch_RemoveSource(XmlRpcDispatch* instance, XmlRpcSource *source)
{
    
    try
    {
        (*instance).removeSource(source);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcDispatch_SetSourceEvents(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned eventMask)
{
    try
    {
        (*instance).setSourceEvents(source, (unsigned)eventMask);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    
}
extern "C" XMLRPC_API void XmlRpcDispatch_Work(XmlRpcDispatch *instance, double msTime)
{
    try
    {
        (*instance).work(msTime);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcDispatch_Exit(XmlRpcDispatch *instance)
{
    try
    {
        (*instance).exit();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcDispatch_Clear(XmlRpcDispatch *instance)
{
    try
    {
        (*instance).clear();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}

//XmlRpcSource
extern "C" XMLRPC_API void XmlRpcSource_Close(XmlRpcSource *instance)
{
    try
    {
        (*instance).close();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API int XmlRpcSource_GetFd(XmlRpcSource *instance)
{
    try
    {
        return ((*instance).getfd());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return -1;
}
extern "C" XMLRPC_API void XmlRpcSource_SetFd(XmlRpcSource *instance, int fd)
{
    try
    {
        (*instance).setfd(fd);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API unsigned char XmlRpcSource_GetKeepOpen(XmlRpcSource *instance)
{
    try
    {
        return ((*instance).getKeepOpen()) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API void XmlRpcSource_SetKeepOpen(XmlRpcSource *instance, bool b)
{
    try
    {
        (*instance).setKeepOpen(b);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API unsigned XmlRpcSource_HandleEvent(XmlRpcSource *instance, unsigned eventType)
{
    try
    {
        return ((*instance).handleEvent(eventType));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return 0;
}

//XmlRpcServerMethod
extern "C" XMLRPC_API XmlRpcServerMethodWrapper *XmlRpcServerMethod_Create(char *name, XmlRpcServer *server)
{
    try
    {
        return (new XmlRpcServerMethodWrapper(std::string(name), server));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API void XmlRpcServerMethod_SetFunc(XmlRpcServerMethodWrapper *instance, XmlRpcServerFUNC func)
{
    try
    {
        (*instance).setFunc(func);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcServerMethod_Execute(XmlRpcServerMethodWrapper *instance, XmlRpcValue *parms, XmlRpcValue *res)
{
    try
    {
        (*instance).execute(*parms, *res);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
    
//XmlRpcServer    
extern "C" XMLRPC_API XmlRpcServer *XmlRpcServer_Create()
{
    try
    {
        return (new XmlRpcServer());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API void XmlRpcServer_AddMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
    try
    {
        (*instance).addMethod(method);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
    try
    {
        (*instance).removeMethod(method);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethodByName(XmlRpcServer *instance, char *name)
{
    try
    {
        (*instance).removeMethod(std::string(name));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API XmlRpcServerMethod *XmlRpcServer_FindMethod(XmlRpcServer *instance, char *name)
{
    try
    {
        return ((*instance).findMethod(std::string(name)));
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}
extern "C" XMLRPC_API unsigned char XmlRpcServer_BindAndListen(XmlRpcServer *instance, int port, int backlog)
{
    try
    {
        return ((*instance).bindAndListen(port, backlog)) ? 1 : 0;
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return false;
}
extern "C" XMLRPC_API void XmlRpcServer_Work(XmlRpcServer *instance, double msTime)
{
    try
    {
        (*instance).work(msTime);
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcServer_Exit(XmlRpcServer *instance)
{
    try
    {
        (*instance).exit();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API void XmlRpcServer_Shutdown(XmlRpcServer *instance)
{
    try
    {
        (*instance).shutdown();
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
}
extern "C" XMLRPC_API int XmlRpcServer_GetPort(XmlRpcServer *instance)
{
    try
    {
        return ((*instance).get_port());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return -1;
}
extern "C" XMLRPC_API XmlRpcDispatch *XmlRpcServer_GetDispatch(XmlRpcServer *instance)
{
    try
    {
        return ((*instance).get_dispatch());
    }
    catch (std::exception& ex)
    {
        XmlRpcUtil::error(ex.what());
    }
    return NULL;
}