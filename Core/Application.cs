using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Geocoding
{
    /// <summary>
    /// Classe Application (contient les paramètres généraux de l'application).
    /// </summary>
    public class Application
    {

        /// <summary>
        /// Id de l'application.
        /// </summary>
        public const string APP_ID = "Geocoding.Core";

        /// <summary>
        /// Objet configuration des traces.
        /// </summary>
        static private TraceSource m_traceSource = new TraceSource(APP_ID);

        /// <summary>
        /// Objet configuration des traces.
        /// </summary>
        static public TraceSource TraceSrc
        {
            get { return m_traceSource; }
        }

        #region DEBUG Trace
        [Conditional("DEBUG")]
        public static void Debug(TraceEventType enventType, int id)
        {
            m_traceSource.TraceEvent(enventType, id);
        }
        [Conditional("DEBUG")]
        public static void Debug(TraceEventType enventType, int id, string message)
        {
            m_traceSource.TraceEvent(enventType, id, message);
        }
        [Conditional("DEBUG")]
        public static void Debug(TraceEventType enventType, int id, string message, params object[] args)
        {
            m_traceSource.TraceEvent(enventType, id, message, args);
        }
        #endregion
    }
}
