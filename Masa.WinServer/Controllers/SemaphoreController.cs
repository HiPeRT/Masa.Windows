﻿using IoT_Server.Helpers;
using IoT_Server.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace IoT_Server.Controllers
{
    public class SemaphoreController : ApiController
    {
        protected static IDictionary<string, SemaphoreStatus> semStatus = new Dictionary<string, SemaphoreStatus>();
        protected static IDictionary<string, SemaphoreCtrl> semCtrl = new Dictionary<string, SemaphoreCtrl>();
        protected int SemResetTimeoutSec = 5;

        [Route("api/semaphore/{semId}/status")]
        [HttpPost]
        //[RequireHttps]
        //[Authorize]
        public IHttpActionResult Status_Post(string semId, [FromBody] SemaphoreStatus status)
        {
            //if (User == null ||
            //       User.Identity == null ||
            //       string.IsNullOrEmpty(User.Identity.Name) ||
            //       !string.Equals(semId, User.Identity.Name))
            //    return Unauthorized();

            if (status == null)
                return BadRequest();

            status.LastUpdateUTC = DateTime.UtcNow;
            lock (semStatus)
            {
                if (semStatus.ContainsKey(semId))
                    semStatus.Remove(semId);
            }
            semStatus.Add(semId, status);


            //LogsController.Log("Semaphore '" + semId + " changed its status");

            return Ctrl_Get(semId);
        } // Status_Post
        
        [Route("api/semaphore/{semId}/status")]
        [HttpGet]
        //[RequireHttps]
        public IHttpActionResult Status_Get(string semId)
        {
            if (string.IsNullOrEmpty(semId))
                return BadRequest();

            lock (semStatus)
            {
                if (!semStatus.ContainsKey(semId))
                    semStatus.Add(semId, new SemaphoreStatus());
            }
            var ret = semStatus[semId];

            // If we don't hear anything for SemResetTimeoutSec, let's switch it off
            if ((DateTime.UtcNow - ret.LastUpdateUTC).TotalSeconds > SemResetTimeoutSec)
                semStatus[semId].Reset();

            return Ok(ret);
        } // Status_Get

        [Route("api/semaphore/{semId}/ctrl")]
        [HttpPost]
        //[Authorize]
        //[RequireHttps]
        public IHttpActionResult Ctrl_Post(string semId, [FromBody] SemaphoreCtrl ctrl)
        {
            if (string.IsNullOrEmpty(ctrl.Key) || !string.Equals(ConfigurationManager.AppSettings["Key"], ctrl.Key))
                return Unauthorized();

            lock (semCtrl)
            {
                if (semCtrl.ContainsKey(semId))
                    semCtrl.Remove(semId);
            }

            ctrl.Key = string.Empty; // Not to shout our pwd all around the world...
            semCtrl.Add(semId, ctrl);

            LogsController.Log("Semaphore '" + semId + "' programmed");

            return Ok();
        } // Ctrl_Post

        [Route("api/semaphore/{semId}/ctrl")]
        [HttpGet]
        //[RequireHttps]
        public IHttpActionResult Ctrl_Get(string semId)
        {
            if (string.IsNullOrEmpty(semId))
                return BadRequest();

            if (!semCtrl.ContainsKey(semId))
                return Ok("");

            var ret = semCtrl[semId];

            return Ok(ret);
        } // Ctrl_Get

    } // class
} // ns
