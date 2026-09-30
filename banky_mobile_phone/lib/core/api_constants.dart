/// ثوابت الاتصال بواجهة برمجة التطبيقات (API Constants)
/// يحتوي على الروابط الأساسية ونقاط النهاية (Endpoints) لربط تطبيق الهاتف بخادم Banky.API
class ApiConstants {
  /// الرابط الأساسي للـ API
  /// ملاحظة للمطورين:
  /// - في حال استخدام محاكي أندرويد (Android Emulator) استخدم: http://10.0.2.2:5021
  /// - في حال استخدام جهاز حقيقي متصل بنفس الشبكة استخدم IP جهازك مثل: http://192.168.1.50:5021
  /// - في حال تشغيل التطبيق على الويب أو سطح المكتب استخدم: http://localhost:5021
  static const String baseUrl = 'http://10.0.114.6:5021';
  // static const String baseUrl = 'https://10.0.114.6:7205';

  // ==========================================
  // مسارات المصادقة والحسابات (Auth Endpoints)
  // ==========================================
  static const String register = '/api/auth/register';
  static const String login = '/api/auth/login';
  static const String profile = '/api/auth/profile';
  static const String submitKyc = '/api/auth/submit-kyc';
  static const String forgotPassword = '/api/auth/forgot-password';
  static const String resetPassword = '/api/auth/reset-password';
  static const String changePassword = '/api/auth/change-password';
  static const String updatePrivacy = '/api/auth/update-privacy';
  static const String regeneratePosAlias = '/api/auth/regenerate-pos-alias';

  // ==========================================
  // مسارات المحافظ المالية (Wallets Endpoints)
  // ==========================================
  static const String getWallets = '/api/wallets';
  static const String createWallet = '/api/wallets/create';
  static const String deposit = '/api/wallets/deposit';
  static const String availableCurrencies = '/api/wallets/available-currencies';

  // ==========================================
  // مسارات التحويلات ونقاط البيع (Transfers & POS)
  // ==========================================
  static const String lookupRecipient = '/api/transfers/lookup-recipient';
  static const String lookupPos = '/api/transfers/lookup-pos';
  static const String transferByPhone = '/api/transfers/by-phone';
  static const String payPos = '/api/transfers/pay-pos';
  static const String calculateExchange = '/api/transfers/calculate-exchange';
  static const String exchangeSelf = '/api/transfers/exchange-self';
  static const String feePreview = '/api/admin/fees/preview';

  // ==========================================
  // مسارات نقاط البيع التابعة للعميل (My POS)
  // ==========================================
  static const String createPos = '/api/pos/create';
  static const String myPosPoints = '/api/pos/my-points';

  // ==========================================
  // مسارات سجل العمليات (Transactions)
  // ==========================================
  static const String getTransactions = '/api/transactions';
}
