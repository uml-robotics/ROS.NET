#include "XmlRpc.h"

using namespace XmlRpc;

namespace XmlRpc
{
	
	typedef void *XmlRpcServerFUNC(XmlRpcValue *params, XmlRpcValue *result);

	class XmlRpcServerMethodWrapper : public XmlRpcServerMethod
	{
	public:
		XmlRpcServerMethodWrapper(const std::string& function_name, XmlRpcServer *s)
			: XmlRpcServerMethod(function_name, s), _name(function_name)
		{}
		virtual ~XmlRpcServerMethodWrapper(){}
		std::string& name()
		{
			return _name;
		}
		virtual void execute(XmlRpcValue &params, XmlRpcValue &result)
		{
			if (!_func)
				return;
			_func(&params, &result);
		}
		virtual std::string help(){return std::string();}
		void setFunc(XmlRpcServerFUNC func)
		{
			_func = func;
		}

	protected:
		std::string _name;
		XmlRpcServerFUNC *_func;
	};
}