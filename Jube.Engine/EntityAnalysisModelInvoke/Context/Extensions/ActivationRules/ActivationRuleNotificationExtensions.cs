/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using System;
    using System.Globalization;
    using System.Text;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Helpers;
    using Models;
    using Newtonsoft.Json;
    using RabbitMQ.Client;

    public static class ActivationRuleNotificationExtensions
    {
        public static void ActivationRuleNotification(this Context context, EntityAnalysisModelActivationRule evaluateActivationRule,
            bool suppressed, IModel rabbitMqChannel)
        {
            if (context.Environment.AppSettings("EnableNotification").Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                if (suppressed || !evaluateActivationRule.EnableNotification || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                {
                    return;
                }

                var notification = new Notification
                {
                    NotificationBody = ReplaceTokens(context, evaluateActivationRule.NotificationBody),
                    NotificationDestination = ReplaceTokens(context, evaluateActivationRule.NotificationDestination),
                    NotificationSubject = ReplaceTokens(context, evaluateActivationRule.NotificationSubject),
                    NotificationTypeId = evaluateActivationRule.NotificationTypeId
                };

                if (context.Environment.AppSettings("AMQP").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    var jsonString = JsonConvert.SerializeObject(notification, context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings);
                    var bodyBytes = Encoding.UTF8.GetBytes(jsonString);
                    rabbitMqChannel.BasicPublish("", "jubeNotifications", null, bodyBytes);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has sent a message to the notification dispatcher as {jsonString}.");
                    }
                }
                else
                {
                    context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Enqueue(notification);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has not sent a message to the internal notification dispatcher because AMQP is not enabled.");
                    }
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has not sent a message as notification disabled.");
                }
            }
        }

        private static string ReplaceTokens(Context context, string message)
        {
            var notificationTokenizationList = NotificationTokenization.ReturnTokens(message);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has found {notificationTokenizationList.Count} tokens in message {message}.");
            }

            foreach (var notificationToken in notificationTokenizationList)
            {
                var notificationTokenValue = "";
                if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(notificationToken, out var valuePayload))
                {
                    notificationTokenValue = valuePayload.ToString();
                }
                else if (context.EntityAnalysisModelInstanceEntryPayload.Abstraction.TryGetValue(notificationToken,
                             out var valueAbstraction))
                {
                    notificationTokenValue = valueAbstraction.ToString(CultureInfo.InvariantCulture);
                }
                else if (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.TryGetValue(notificationToken,
                             out var valueTtlCounter))
                {
                    notificationTokenValue = valueTtlCounter.ToString();
                }
                else if (
                    context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.TryGetValue(notificationToken,
                        out var valueAbstractionCalculation))
                {
                    notificationTokenValue = valueAbstractionCalculation
                        .ToString(CultureInfo.InvariantCulture);
                }

                var notificationReplaceToken = $"[@{notificationToken}@]";
                message = message.Replace(notificationReplaceToken, notificationTokenValue);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has finalized notification message {message}.");
                }
            }

            return message;
        }
    }
}
