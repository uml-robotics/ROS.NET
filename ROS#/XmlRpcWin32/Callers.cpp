#include <windows.h>
#include "Callers.h"
#include "EricRulz.h"
using namespace XmlRpc;
//bullshit check
extern "C" XMLRPC_API int IntegerEcho(int val)
{ 
	return val;
}
FuncPtr CBREF;
extern "C" XMLRPC_API void IntegerEchoFunctionPtr(FuncPtr ptr)
{
	CBREF = ptr;
}
extern "C" XMLRPC_API bool IntegerEchoRepeat(int val)
{
	if (!CBREF)
		return false;	
	CBREF(val);
	return true;
}
extern "C" XMLRPC_API void StringPassingTest(const char *str)
{
	XmlRpcUtil::log(0, "%s", str);
}
EricRulz OUTREF;	
//general
extern "C" XMLRPC_API void SetStringOutFunc(EricRulz fptr)
{
	OUTREF = fptr;
}

//client
extern "C" XMLRPC_API XmlRpcClient* XmlRpcClient_Create(const char *host, int port, const char *uri)
{	
	XmlRpcUtil::log(0, "Making new client http://%s:%d%s", host, port, uri);
	return new XmlRpcClient(host, port, uri);
}
extern "C" XMLRPC_API void XmlRpcClient_Close(XmlRpcClient* instance)
{
	if (!instance) return;
	instance->close();
}
extern "C" XMLRPC_API bool XmlRpcClient_Execute(XmlRpcClient* instance, const char* method, XmlRpcValue const& parameters, XmlRpcValue& result)
{
	return instance->execute(method, parameters, result);
}
extern "C" XMLRPC_API bool XmlRpcClient_ExecuteNonBlock(XmlRpcClient* instance, const char* method, XmlRpcValue const& parameters)
{
	return instance->executeNonBlock(method, parameters);
}
extern "C" XMLRPC_API bool XmlRpcClient_ExecuteCheckDone(XmlRpcClient* instance, XmlRpcValue& result)
{
	return instance->executeCheckDone(result);
}
extern "C" XMLRPC_API unsigned XmlRpcClient_HandleEvent(XmlRpcClient* instance, unsigned eventType)
{
	return instance->handleEvent(eventType);
}
extern "C" XMLRPC_API bool XmlRpcClient_IsFault(XmlRpcClient* instance)
{
	return instance->isFault();
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetHost(XmlRpcClient* instance)
{
	return instance->getHost().c_str();
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetUri(XmlRpcClient* instance)
{
	return instance->getUri().c_str();
}
extern "C" XMLRPC_API int XmlRpcClient_GetPort(XmlRpcClient* instance)
{
	return instance->getPort();
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetRequest(XmlRpcClient* instance)
{
	return instance->_request.c_str();
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetHeader(XmlRpcClient* instance)
{
	return instance->_header.c_str();
}
extern "C" XMLRPC_API const char* XmlRpcClient_GetResponse(XmlRpcClient* instance)
{
	return instance->_response.c_str();
}
extern "C" XMLRPC_API int XmlRpcClient_GetSendAttempts(XmlRpcClient* instance)
{
	return instance->_sendAttempts;
}
extern "C" XMLRPC_API int XmlRpcClient_GetBytesWritten(XmlRpcClient* instance)
{
	return instance->_bytesWritten;
}
extern "C" XMLRPC_API bool XmlRpcClient_GetExecuting(XmlRpcClient* instance)
{
	return instance->_executing;
}
extern "C" XMLRPC_API bool XmlRpcClient_GetEOF(XmlRpcClient* instance, XmlRpcValue& result)
{
	return instance->_eof;
}
extern "C" XMLRPC_API int XmlRpcClient_GetContentLength(XmlRpcClient* instance)
{
	return instance->_contentLength;
}
extern "C" XMLRPC_API XmlRpcDispatch* XmlRpcClient_GetXmlRpcDispatch(XmlRpcClient* instance)
{
	return &instance->_disp;
}

//value
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create1()
{
	return new XmlRpcValue();
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create2(bool value)
{
	return new XmlRpcValue(value);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create3(int value)
{
	return new XmlRpcValue(value);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create4(double value)
{	
	return new XmlRpcValue(value);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create5(const char* value)
{	
	return new XmlRpcValue(value);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create6(void* value, int nBytes)
{
	return new XmlRpcValue(value, nBytes);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create7(const char* xml, int offset)
{
	std::string str = std::string(xml);
	return new XmlRpcValue(&str, offset);
}
extern "C" XMLRPC_API XmlRpcValue *XmlRpcValue_Create8(XmlRpcValue *rhs)
{	
	return new XmlRpcValue(rhs);
}
extern "C" XMLRPC_API void XmlRpcValue_Clear(XmlRpcValue* instance)
{
	if (!instance) return;
	instance->clear();
}
extern "C" XMLRPC_API bool XmlRpcValue_Valid(XmlRpcValue* instance)
{
	return instance->valid();
}
extern "C" XMLRPC_API int XmlRpcValue_Type(XmlRpcValue* instance)
{
	return (int)instance->getType();
}
extern "C" XMLRPC_API int XmlRpcValue_Size(XmlRpcValue* instance)
{
	return instance->size();
}
extern "C" XMLRPC_API void XmlRpcValue_SetSize(XmlRpcValue* instance, int size)
{
	if (!instance) return;
	instance->setSize(size);
}
extern "C" XMLRPC_API bool XmlRpcValue_HasMember(XmlRpcValue* instance, const char* name)
{
	return instance->hasMember(name);
}
extern "C" XMLRPC_API XmlRpcValue* XmlRpcValue_Get1(XmlRpcValue* instance, int key)
{
	return &instance->operator[](key);
}
extern "C" XMLRPC_API XmlRpcValue* XmlRpcValue_Get2(XmlRpcValue* instance, const char* key)
{
	return &instance->operator[](key);
}
extern "C" XMLRPC_API void XmlRpcValue_Set1(XmlRpcValue* instance, int key, const char *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set2(XmlRpcValue* instance, const char* key, const char *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set3(XmlRpcValue* instance, int key, XmlRpcValue *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set4(XmlRpcValue* instance, const char* key, XmlRpcValue *value)
{	
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set5(XmlRpcValue* instance, int key, int *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set6(XmlRpcValue* instance, const char* key, int *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set7(XmlRpcValue* instance, int key, bool *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set8(XmlRpcValue* instance, const char* key, bool *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set9(XmlRpcValue* instance, int key, double *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set10(XmlRpcValue* instance, const char* key, double *value)
{
	if (!instance) return;
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API int XmlRpcValue_GetInt0(XmlRpcValue* instance)
{
	return instance->operator int &();
}
extern "C" XMLRPC_API int XmlRpcValue_GetInt1(XmlRpcValue* instance, int key)
{
	return (&instance->operator[](key))->operator int &();
}
extern "C" XMLRPC_API int XmlRpcValue_GetInt2(XmlRpcValue* instance, const char* key)
{
	return (&instance->operator[](key))->operator int &();
}
extern "C" XMLRPC_API const char* XmlRpcValue_GetString0(XmlRpcValue* instance)
{
	return instance->operator std::string &().c_str();
}
extern "C" XMLRPC_API const char* XmlRpcValue_GetString1(XmlRpcValue* instance, int key)
{
	return (&instance->operator[](key))->operator std::string &().c_str();
}
extern "C" XMLRPC_API const char* XmlRpcValue_GetString2(XmlRpcValue* instance, const char* key)
{
	return (&instance->operator[](key))->operator std::string &().c_str();
}
extern "C" XMLRPC_API bool XmlRpcValue_GetBool0(XmlRpcValue* instance)
{
	return instance->operator bool &();
}
extern "C" XMLRPC_API bool XmlRpcValue_GetBool1(XmlRpcValue* instance, int key)
{
	return (&instance->operator[](key))->operator bool &();
}
extern "C" XMLRPC_API bool XmlRpcValue_GetBool2(XmlRpcValue* instance, const char* key)
{
	return (&instance->operator[](key))->operator bool &();
}
extern "C" XMLRPC_API double XmlRpcValue_GetDouble0(XmlRpcValue* instance)
{	
	return instance->operator double &();
}
extern "C" XMLRPC_API double XmlRpcValue_GetDouble1(XmlRpcValue* instance, int key)
{
	return (&instance->operator[](key))->operator double &();
}
extern "C" XMLRPC_API double XmlRpcValue_GetDouble2(XmlRpcValue* instance, const char* key)
{
	return (&instance->operator[](key))->operator double &();
}
extern "C" XMLRPC_API void XmlRpcValue_Dump(XmlRpcValue* instance)
{
	if (!instance) return;
	XmlRpcUtil::log(0, "C++Dump: ptr=%d\ttype=%d\tsize=%d", (int)instance, instance->_type, instance->size());
}

//dispatch
extern "C" XMLRPC_API XmlRpcDispatch *XmlRpcDispatch_Create()
{
	return new XmlRpcDispatch();
}
extern "C" XMLRPC_API void XmlRpcDispatch_Close(XmlRpcDispatch *instance)
{
	if (!instance) return;
	instance->~XmlRpcDispatch();
}
extern "C" XMLRPC_API void XmlRPcDispatch_AddSource(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned int eventMask)
{
	if (!instance) return;
	instance->addSource(source, eventMask);
}
extern "C" XMLRPC_API void XmlRpcDispatch_RemoveSource(XmlRpcDispatch* instance, XmlRpcSource *source)
{
	if (!instance) return;
	instance->removeSource(source);
}
extern "C" XMLRPC_API void XmlRpcDispatch_SetSourceEvents(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned int eventMask)
{
	if (!instance) return;
	instance->setSourceEvents(source, eventMask);
}
extern "C" XMLRPC_API void XmlRpcDispatch_Work(XmlRpcDispatch *instance, double msTime)
{
	if (!instance) return;
	instance->work(msTime);
}
extern "C" XMLRPC_API void XmlRpcDispatch_Exit(XmlRpcDispatch *instance)
{
	if (!instance) return;
	instance->exit();
}
extern "C" XMLRPC_API void XmlRPcDispatch_Clear(XmlRpcDispatch *instance)
{
	if (!instance) return;
	instance->clear();
}

//XmlRpcSource
extern "C" XMLRPC_API void XmlRpcSource_Close(XmlRpcSource *instance)
{
	if (!instance) return;
	instance->close();
}
extern "C" XMLRPC_API int XmlRpcSource_GetFd(XmlRpcSource *instance)
{
	return instance->getfd();
}
extern "C" XMLRPC_API void XmlRpcSource_SetFd(XmlRpcSource *instance, int fd)
{
	if (!instance) return;
	instance->setfd(fd);
}
extern "C" XMLRPC_API bool XmlRpcSource_GetKeepOpen(XmlRpcSource *instance)
{
	return instance->getKeepOpen();
}
extern "C" XMLRPC_API void XmlRpcSource_SetKeepOpen(XmlRpcSource *instance, bool b)
{
	if (!instance) return;
	instance->setKeepOpen(b);
}
extern "C" XMLRPC_API unsigned XmlRPcSource_HandleEvent(XmlRpcSource *instance, unsigned eventType)
{
	return instance->handleEvent(eventType);
}

//XmlRpcServerMethod
extern "C" XMLRPC_API XmlRpcServerMethodWrapper *XmlRpcServerMethod_Create(char *name, XmlRpcServer *server)
{
	return new XmlRpcServerMethodWrapper(std::string(name), server);
}
extern "C" XMLRPC_API void XmlRpcServerMethod_SetFunc(XmlRpcServerMethodWrapper *instance, XmlRpcServerFUNC func)
{
	if (!instance) return;
	instance->setFunc(func);
}
extern "C" XMLRPC_API void XmlRpcServerMethod_Execute(XmlRpcServerMethodWrapper *instance, XmlRpcValue *parms, XmlRpcValue *res)
{
	if (!instance) return;
	instance->execute(*parms, *res);
}
	
//XmlRpcServer	
extern "C" XMLRPC_API XmlRpcServer *XmlRpcServer_Create()
{
	return new XmlRpcServer();
}
extern "C" XMLRPC_API void XmlRpcServer_AddMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
	if (!instance) return;
	instance->addMethod(method);
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
	if (!instance) return;
	instance->removeMethod(method);
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethodByName(XmlRpcServer *instance, char *name)
{
	if (!instance) return;
	instance->removeMethod(std::string(name));
}
extern "C" XMLRPC_API XmlRpcServerMethod *XmlRpcServer_FindMethod(XmlRpcServer *instance, char *name)
{
	return instance->findMethod(std::string(name));
}
extern "C" XMLRPC_API bool XmlRpcServer_BindAndListen(XmlRpcServer *instance, int port, int backlog)
{
	return instance->bindAndListen(port, backlog);
}
extern "C" XMLRPC_API void XmlRpcServer_Work(XmlRpcServer *instance, double msTime)
{
	if (!instance) return;
	instance->work(msTime);
}
extern "C" XMLRPC_API void XmlRpcServer_Exit(XmlRpcServer *instance)
{
	if (!instance) return;
	instance->exit();
}
extern "C" XMLRPC_API void XmlRpcServer_Shutdown(XmlRpcServer *instance)
{
	if (!instance) return;
	instance->shutdown();
}
extern "C" XMLRPC_API int XmlRpcServer_GetPort(XmlRpcServer *instance)
{
	return instance->get_port();
}
extern "C" XMLRPC_API XmlRpcDispatch *XmlRpcServer_GetDispatch(XmlRpcServer *instance)
{
	return instance->get_dispatch();
}