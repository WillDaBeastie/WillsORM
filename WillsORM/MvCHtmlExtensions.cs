using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WillsORM
{
	public static class MvcHtmlExtensions
	{
		public static IHtmlString WillsORMValidation(this HtmlHelper helper, Validation validation)
		{
			StringBuilder sb = new StringBuilder();

			var manFields = validation.MandatoryFields;

			if (manFields != null)
			{
				sb.Append("\n<script language=\"javascript\" type=\"text/javascript\">");
				sb.Append("\n mandVals = [  ");
				foreach (string manField in manFields)
				{
					sb.AppendFormat("\"{0}\",", manField);
				}

				sb.Append(" ];\n ");

				if (validation.MandatoryFields2 != null)
				{
					sb.Append("\n mandVals2 = [  ");
					foreach (string manField in validation.MandatoryFields2)
					{
						sb.AppendFormat("\"{0}\",", manField);
					}
				}

				sb.Append(" ];\n	</script> \n");
			}

			if (validation.ValidationErrorReport != null)
			{
				if (validation.ValidationErrorReport.Count > 0)
				{
					sb.Append(" <div class=\"error\"> ");
					sb.Append(" <h3>There was an error on the form</h3> ");
					sb.Append(" 	<ul id=\"validation\"> ");

					foreach (KeyValuePair<string, string> kvp in validation.ValidationErrorReport)
					{
						sb.AppendFormat("<li><span class=\"_key\">{0}</span> <span class=\"_msg\">{1}</span></li>", kvp.Key, kvp.Value);
					}

					sb.Append(" 	</ul>");
					sb.Append(" 	</div> ");
				}
			}

			if (helper.ViewContext.Controller.TempData["message"] != null)
			{
				helper.ViewBag.Message = (string)helper.ViewContext.Controller.TempData["message"];
			}
			//if (!string.IsNullOrEmpty(Model.TempMessage))
			//{
			//	ViewBag.Message = Model.TempMessage;
			//}
			if (helper.ViewBag.Message != null)
			{
				sb.AppendFormat("<p class=\"feedback\">{0}</p>", helper.ViewBag.Message);
			}

			return helper.Raw(sb.ToString());
		}
	}
}
