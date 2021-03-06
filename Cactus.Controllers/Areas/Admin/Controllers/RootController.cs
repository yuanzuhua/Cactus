﻿using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Cactus.Common;
using Cactus.Model.Sys;
using Cactus.Model.Sys.Enums;
using Cactus.Controllers.Expand;
using Cactus.Controllers.Filters;
using Cactus.Model.Other;

namespace Cactus.Controllers.Areas.Admin.Controllers
{
    [Group(Title = "个人中心",Icon = "fa-file",IsShow=false)]
    public class RootController : PowerBaseController
    {
        [Power(ModuleName = "myCenter",Title = "个人中心", actionEnum=EnumsModel.ActionEnum.Show)]
        public ActionResult Index()
        {
            ViewData["logFileCount"] = Directory.GetFiles(HIO.logDirPath).Count();
            return View();
        }
        //参数面板
        [Power(ModuleName = "myCenter",actionEnum = EnumsModel.ActionEnum.Show)]
        public ActionResult ZPanel()
        {
            string OS = "";
#if Linux
            OS="Linux";
#else
            OS = "Windows";
#endif
            ViewData["OSString"] = OS;
            ViewData["AssemblyVersion"] = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            return PartialView("_ZPanel");
        }
        //初步完成
        #region 个人中心

        [HttpGet]
        public ActionResult Logout()
        {
            string token = CookieHelper.GetCookieValue("Admin");
            //移除缓存的登录用户信息
            //CacheHelper.RemoveCache(Constant.CacheKey.LoginAdminInfoCacheKey + "_" + token);
            base.cacheService.Remove(Constant.CacheKey.LoginAdminInfoCacheKey + "_" + token);
            //用户注销
            //FormsAuthentication.SignOut();
            CookieHelper.ClearCookie("Admin");
            base.logService.WriteLog(LoginUser.User_Id, "退出系统");
            if (HttpContext.Request.IsAjaxRequest())
            {
                return Json(new ResultModel
                {
                    pass = true,
                    msg = "退出成功"
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Redirect("/AdminLogin/Index");
            }
        }
        public ActionResult CenterUser()
        {
            return View("CenterUser");
        }
        [HttpGet]
        [Power(ModuleName = "myCenter", actionEnum = EnumsModel.ActionEnum.Edit)]
        public ActionResult CenterAlterPwd()
        {
            return View();
        }
        [HttpPost]
        [Power(ModuleName = "myCenter", actionEnum = EnumsModel.ActionEnum.Edit)]
        public ActionResult CenterAlterPwd(string pwded, string pwding)
        {
            pwded = pwded.Trim();
            pwding = pwding.Trim();
            if (string.IsNullOrEmpty(pwded)) { return Json(new ResultModel { msg = "旧密码为空", pass = false }); }
            if (string.IsNullOrEmpty(pwding)) { return Json(new ResultModel { msg = "新密码为空", pass = false }); }
            if (pwded == pwding)
            {
                return Json(new ResultModel { msg = "旧密码与新密码相同", pass = false });
            }
            if (LoginUser.Password == CryptoHelper.MD5Encrypt(pwded))
            {
                var act = this.userService.Find(LoginUser.User_Id);
                act.Password = CryptoHelper.MD5Encrypt(pwding);
                this.userService.Update(act);
                if (Constant.CacheKey.List[Constant.CacheKey.LoginAdminInfoCacheKey].Count() > 0)
                {
                    base.cacheService.Remove(Constant.CacheKey.LoginAdminInfoCacheKey + "_" + m_token);
                }
                return Json(new ResultModel { msg = "操作成功", pass = true });
            }
            else
            {
                return Json(new { msg = "旧密码错误", pass = false });
            }
        }
        [Power(ModuleName = "myCenter",  actionEnum = EnumsModel.ActionEnum.Edit)]
        public ActionResult CenterAlterFace()
        {
            if (Request.HttpMethod == "GET")
            {
                return View();
            }
            else
            {
                var avatarFile = Request.Files["AvatarFile"];
                if (avatarFile != null)
                {
                    if (!System.IO.Directory.Exists(MyPath.TempPath))
                    {
                        System.IO.Directory.CreateDirectory(MyPath.TempPath);
                    }
                    var avatarName = avatarFile.FileName;
                    var avatarExt = Path.GetExtension(avatarName);
                    //保存原图
                    var savePath = Path.Combine(MyPath.TempPath, avatarName);
                    if (WebHelper.saveUploadFile(avatarFile, savePath, Config.ImgExtensions.Split('*'), this.pathConfig.dic["avatar"].FileSize))
                    {
                        if (!System.IO.Directory.Exists(this.pathConfig.dic["avatar"].DirPath))
                        {
                            System.IO.Directory.CreateDirectory(this.pathConfig.dic["avatar"].DirPath);
                        }
                        //缩略图路径
                        var thumbPath = Path.Combine(this.pathConfig.dic["avatar"].DirPath, "Avatar_" + LoginUser.User_Id + avatarExt);
                        //生成头像缩略图
                        ImageHelper.MakeThumbnailImage(savePath, thumbPath, 48, 48, "HW");
                        LoginUser.Avatar = this.pathConfig.dic["avatar"].WebPath + "/" + "Avatar_" + LoginUser.User_Id + avatarExt;
                        System.IO.File.Delete(savePath);
                        Model.Sys.User u = this.userService.Find(LoginUser.User_Id);
                        if (base.Config.DefaultAvatar != u.Avatar)
                        {
                            string _syspath = HIO.SysPathParse(MyPath.AppPath, u.Avatar, true);
                            try
                            {
                                System.IO.File.Delete(_syspath);
                            }
                            catch (Exception e)
                            {
                                HIO.WriteLog(e);
                            }
                        }
                        u.Avatar = LoginUser.Avatar;
                        this.userService.Update(u);
                        return Json(new ResultModel { pass = true, msg = "上传成功", append = new { url = LoginUser.Avatar } });
                    }
                    else
                    {
                        return Json(new ResultModel { msg = "上传文件错误,注意文件大小" + this.pathConfig.dic["avatar"].FileSize + "kb以内或文件类型为" + Config.ImgExtensions, pass = false });
                    }
                }
                else
                {
                    return Json(new ResultModel { msg = "上传文件错误", pass = false });
                }
            }
        }
        [HttpGet]
        [Power(ModuleName = "myCenter",  actionEnum = EnumsModel.ActionEnum.Edit)]
        public ActionResult CenterAlterInfo()
        {
            return View();
        }
        [HttpPost]
        [Power(ModuleName = "myCenter",  actionEnum = EnumsModel.ActionEnum.Edit)]
        public ActionResult CenterAlterInfo(User u)
        {
            try
            {
                u.NickName = u.NickName.Trim();
                if (string.IsNullOrEmpty(u.NickName))
                {
                    return Json(new ResultModel { pass = false, msg = "昵称为空" });
                }
                if (string.IsNullOrEmpty(u.Email))
                {
                    return Json(new ResultModel { pass = false, msg = "邮箱为空" });
                }
                if (string.IsNullOrEmpty(u.Phone))
                {
                    return Json(new ResultModel { pass = false, msg = "手机为空" });
                }
                if (string.IsNullOrEmpty(u.Qq))
                {
                    return Json(new ResultModel { pass = false, msg = "qq为空" });
                }
                var act = this.userService.Find(LoginUser.User_Id);
                act.NickName = u.NickName;
                act.Email = u.Email;
                act.Phone = u.Phone;
                act.Qq = u.Qq;
                this.userService.Update(act);
                if (Constant.CacheKey.List[Constant.CacheKey.LoginAdminInfoCacheKey].Count() > 0)
                {
                    base.cacheService.Remove(Constant.CacheKey.LoginAdminInfoCacheKey + "_" + m_token);
                }
                return Json(new ResultModel { pass = true, msg = "修改成功" });
            }
            catch (Exception e)
            {
                return Json(new ResultModel { pass = false, msg = e.Message });
            }
        }

        #endregion
    }
}
