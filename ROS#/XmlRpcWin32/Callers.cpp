#include <windows.h>
#include "Callers.h"
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

//client
extern "C" XMLRPC_API XmlRpcClient* XmlRpcClient_Create(const char *host, int port, const char *uri)
{
	return new XmlRpc::XmlRpcClient(host, port, uri);
}
extern "C" XMLRPC_API void XmlRpcClient_Close(XmlRpcClient* instance)
{	
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
	instance->operator[](key) = new XmlRpcValue(value);
}
extern "C" XMLRPC_API void XmlRpcValue_Set2(XmlRpcValue* instance, const char* key, const char *value)
{
	instance->operator[](key) = new XmlRpcValue(value);
}
extern "C" XMLRPC_API void XmlRpcValue_Set3(XmlRpcValue* instance, int key, XmlRpcValue *value)
{
	instance->operator[](key) = value;
}
extern "C" XMLRPC_API void XmlRpcValue_Set4(XmlRpcValue* instance, const char* key, XmlRpcValue *value)
{	
	instance->operator[](key) = value;
}

//dispatch
extern "C" XMLRPC_API XmlRpcDispatch *XmlRpcDispatch_Create()
{
	return new XmlRpcDispatch();
}
extern "C" XMLRPC_API void XmlRpcDispatch_Close(XmlRpcDispatch *instance)
{
	instance->~XmlRpcDispatch();
}
extern "C" XMLRPC_API void XmlRPcDispatch_AddSource(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned int eventMask)
{
	instance->addSource(source, eventMask);
}
extern "C" XMLRPC_API void XmlRpcDispatch_RemoveSource(XmlRpcDispatch* instance, XmlRpcSource *source)
{
	instance->removeSource(source);
}
extern "C" XMLRPC_API void XmlRpcDispatch_SetSourceEvents(XmlRpcDispatch* instance, XmlRpcSource *source, unsigned int eventMask)
{
	instance->setSourceEvents(source, eventMask);
}
extern "C" XMLRPC_API void XmlRpcDispatch_Work(XmlRpcDispatch *instance, double msTime)
{
	instance->work(msTime);
}
extern "C" XMLRPC_API void XmlRpcDispatch_Exit(XmlRpcDispatch *instance)
{
	instance->exit();
}
extern "C" XMLRPC_API void XmlRPcDispatch_Clear(XmlRpcDispatch *instance)
{
	instance->clear();
}

//XmlRpcSource
extern "C" XMLRPC_API void XmlRpcSource_Close(XmlRpcSource *instance)
{
	instance->close();
}
extern "C" XMLRPC_API int XmlRpcSource_GetFd(XmlRpcSource *instance)
{
	return instance->getfd();
}
extern "C" XMLRPC_API void XmlRpcSource_SetFd(XmlRpcSource *instance, int fd)
{
	instance->setfd(fd);
}
extern "C" XMLRPC_API bool XmlRpcSource_GetKeepOpen(XmlRpcSource *instance)
{
	return instance->getKeepOpen();
}
extern "C" XMLRPC_API void XmlRpcSource_SetKeepOpen(XmlRpcSource *instance, bool b)
{
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
	instance->setFunc(func);
}
extern "C" XMLRPC_API void XmlRpcServerMethod_Execute(XmlRpcServerMethodWrapper *instance, XmlRpcValue *parms, XmlRpcValue *res)
{
	instance->execute(*parms, *res);
}
	
//XmlRpcServer	
extern "C" XMLRPC_API XmlRpcServer *XmlRpcServer_Create()
{
	return new XmlRpcServer();
}
extern "C" XMLRPC_API void XmlRpcServer_AddMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
	instance->addMethod(method);
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethod(XmlRpcServer *instance, XmlRpcServerMethod *method)
{
	instance->removeMethod(method);
}
extern "C" XMLRPC_API void XmlRpcServer_RemoveMethodByName(XmlRpcServer *instance, char *name)
{
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
	instance->work(msTime);
}
extern "C" XMLRPC_API void XmlRpcServer_Exit(XmlRpcServer *instance)
{
	instance->exit();
}
extern "C" XMLRPC_API void XmlRpcServer_Shutdown(XmlRpcServer *instance)
{
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