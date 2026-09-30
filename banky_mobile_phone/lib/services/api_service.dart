import 'dart:convert';
import 'dart:developer';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../core/api_constants.dart';
import '../models/user_model.dart';
import '../models/wallet_model.dart';
import '../models/transaction_model.dart';
import '../models/pos_model.dart';

/// خدمة الاتصال بخادم الـ API (Api Service)
/// تنفذ كافة عمليات إرسال واستقبال البيانات مع الـ Banky.API عبر HTTP وتحافظ على توكن الجلسة JWT
class ApiService {
  static const String _tokenPrefKey = 'banky_jwt_token';

  /// استرجاع توكن الـ JWT المخزن في الهاتف
  static Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_tokenPrefKey);
  }

  /// حفظ توكن الـ JWT
  static Future<void> saveToken(String token) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_tokenPrefKey, token);
  }

  /// حذف توكن الـ JWT عند تسجيل الخروج
  static Future<void> clearToken() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_tokenPrefKey);
  }

  /// تجهيز الترويسات الافتراضية مع تضمين رمز الـ Bearer Token
  static Future<Map<String, String>> _getHeaders() async {
    final token = await getToken();
    return {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  // ==========================================
  // 1. عمليات المصادقة والحسابات (Auth)
  // ==========================================

  /// تسجيل حساب جديد
  static Future<Map<String, dynamic>> register({
    required String fullName,
    required String email,
    required String phone,
    required String password,
  }) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.register}');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'fullName': fullName,
          'email': email,
          'phone': phone,
          'password': password,
        }),
      );

      final data = jsonDecode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        if (data['data'] != null && data['data']['token'] != null) {
          await saveToken(data['data']['token']);
        }
        return {
          'success': true,
          'message': data['message'],
          'data': data['data'],
        };
      }
      return {
        'success': false,
        'message': data['message'] ?? 'فشل إنشاء الحساب',
      };
    } catch (e) {
      return {'success': false, 'message': 'تعذر الاتصال بالخادم: $e'};
    }
  }

  /// تسجيل الدخول
  Future<Map<String, dynamic>> login(String identifier, String password) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.login}');
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'identifier': identifier, 'password': password}),
      );

      final data = jsonDecode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        if (data['data'] != null && data['data']['token'] != null) {
          await saveToken(data['data']['token']);
        }
        return {
          'success': true,
          'message': data['message'],
          'data': data['data'],
        };
      }
      return {
        'success': false,
        'message': data['message'] ?? 'بيانات الدخول غير صحيحة',
      };
    } catch (e) {
      return {'success': false, 'message': 'تعذر الاتصال بالخادم: $e'};
    }
  }

  /// استرجاع الملف الشخصي
  static Future<UserModel?> getProfile() async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.profile}');
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] != null) {
          return UserModel.fromJson(data['data']);
        }
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  /// رفع وثائق التوثيق KYC (صورة البطاقة الشخصية الوجهين)
  static Future<Map<String, dynamic>> submitKyc({
    required String idFrontBase64,
    required String idBackBase64,
  }) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.submitKyc}');
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'idFrontImageBase64OrPath': idFrontBase64,
          'idBackImageBase64OrPath': idBackBase64,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? 'تم رفع الوثائق',
      };
    } catch (e) {
      return {'success': false, 'message': 'فشل رفع الوثائق: $e'};
    }
  }

  /// طلب استعادة كلمة المرور (نسيت كلمة المرور)
  static Future<Map<String, dynamic>> forgotPassword(String identifier) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.forgotPassword}',
      );
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'identifier': identifier}),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'demoToken': data['demoToken'],
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  /// إعادة تعيين كلمة المرور بواسطة رمز التحقق
  static Future<Map<String, dynamic>> resetPassword({
    required String identifier,
    required String resetToken,
    required String newPassword,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.resetPassword}',
      );
      final response = await http.post(
        url,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'identifier': identifier,
          'resetToken': resetToken,
          'newPassword': newPassword,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  /// تغيير كلمة المرور للمستخدم المسجل دخوله
  static Future<Map<String, dynamic>> changePassword({
    String? currentPassword,
    required String newPassword,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.changePassword}',
      );
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'currentPassword': currentPassword,
          'newPassword': newPassword,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  /// تحديث إعدادات الخصوصية والأمان (إخفاء الاسم، إخفاء الهاتف على POS، البصمة)
  static Future<Map<String, dynamic>> updatePrivacy({
    required bool hideFullName,
    required bool hidePhoneOnPos,
    required bool isBiometricEnabled,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.updatePrivacy}',
      );
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'hideFullName': hideFullName,
          'hidePhoneOnPos': hidePhoneOnPos,
          'isBiometricEnabled': isBiometricEnabled,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'user': data['data'] != null ? UserModel.fromJson(data['data']) : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  /// توليد وتجديد الرقم البديل لنقاط البيع
  static Future<Map<String, dynamic>> regeneratePosAlias() async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.regeneratePosAlias}',
      );
      final headers = await _getHeaders();
      final response = await http.post(url, headers: headers);

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'newAlias': data['newAlias'] ?? '',
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  // ==========================================
  // 2. عمليات المحافظ المالية (Wallets)
  // ==========================================

  /// استرجاع محافظ العميل
  static Future<List<WalletModel>> getWallets() async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.getWallets}',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] is List) {
          return (data['data'] as List)
              .map((w) => WalletModel.fromJson(w))
              .toList();
        }
      }
      return [];
    } catch (_) {
      return [];
    }
  }

  /// فتح محفظة جديدة بعملة معتمدة
  static Future<Map<String, dynamic>> createWallet(String currencyCode) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.createWallet}',
      );
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({'currencyCode': currencyCode}),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'wallet': data['data'] != null
            ? WalletModel.fromJson(data['data'])
            : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  /// استرجاع العملات المتاحة
  static Future<List<CurrencyModel>> getAvailableCurrencies() async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.availableCurrencies}',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);
      log(response.body.toString());
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] is List) {
          return (data['data'] as List)
              .map((c) => CurrencyModel.fromJson(c))
              .toList();
        }
      }
      return [];
    } catch (_) {
      return [];
    }
  }

  /// إيداع وتغذية رصيد في محفظة
  static Future<Map<String, dynamic>> deposit({
    required String accountNumber,
    required double amount,
    String? note,
  }) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.deposit}');
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'accountNumber': accountNumber,
          'amount': amount,
          'note': note,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
      };
    } catch (e) {
      return {'success': false, 'message': 'خطأ في الاتصال: $e'};
    }
  }

  // ==========================================
  // 3. عمليات التحويلات والدفع (Transfers & POS)
  // ==========================================

  /// الاستعلام عن مستلم برقم الهاتف
  static Future<RecipientLookupResult?> lookupRecipient({
    required String phone,
    required String currencyCode,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.lookupRecipient}?phone=$phone&currencyCode=$currencyCode',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] != null) {
          return RecipientLookupResult.fromJson(data['data']);
        }
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  /// تنفيذ التحويل المالي لمشترك برقم الهاتف
  static Future<Map<String, dynamic>> transferByPhone({
    required String receiverPhone,
    required String currencyCode,
    required double amount,
    String? note,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.transferByPhone}',
      );
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'receiverPhone': receiverPhone,
          'currencyCode': currencyCode,
          'amount': amount,
          'note': note,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'transaction': data['data'] != null
            ? TransactionModel.fromJson(data['data'])
            : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'فشل تنفيذ التحويل: $e'};
    }
  }

  /// الاستعلام عن نقطة بيع بواسطة الكود
  static Future<PosLookupResult?> lookupPos(String posCode) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.lookupPos}?posCode=$posCode',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] != null) {
          return PosLookupResult.fromJson(data['data']);
        }
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  /// تنفيذ الشراء والدفع لنقطة بيع
  static Future<Map<String, dynamic>> payToPos({
    required String posCode,
    required String currencyCode,
    required double amount,
    String? note,
  }) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.payPos}');
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'posCode': posCode,
          'currencyCode': currencyCode,
          'amount': amount,
          'note': note,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'transaction': data['data'] != null
            ? TransactionModel.fromJson(data['data'])
            : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'فشل الدفع: $e'};
    }
  }

  /// حساب سعر الصرف والمبلغ المستلم قبل التحويل بين الحسابات
  static Future<Map<String, dynamic>?> calculateExchange({
    required String fromCurrency,
    required String toCurrency,
    required double amount,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.calculateExchange}?fromCurrency=$fromCurrency&toCurrency=$toCurrency&amount=$amount',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] != null) {
          return data['data'];
        }
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  /// تنفيذ التحويل والمصارفة بين محافظ العميل الخاصة
  static Future<Map<String, dynamic>> exchangeSelf({
    required String fromCurrencyCode,
    required String toCurrencyCode,
    required double amount,
    String? note,
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.exchangeSelf}',
      );
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'fromCurrencyCode': fromCurrencyCode,
          'toCurrencyCode': toCurrencyCode,
          'amount': amount,
          'note': note,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'transaction': data['data'] != null
            ? TransactionModel.fromJson(data['data'])
            : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'فشل المصارفة والتحويل: $e'};
    }
  }

  /// معاينة وحساب الرسوم والعمولة التقديرية لأي عملية مصرفية
  static Future<Map<String, dynamic>?> calculateFeePreview({
    required String operationType,
    required double amount,
    String currency = 'YER',
  }) async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.feePreview}?type=$operationType&amount=$amount&currency=$currency',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] != null) {
          return data['data'];
        }
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  // ==========================================
  // 4. نقاط البيع التابعة للعميل (My POS)
  // ==========================================

  /// إنشاء نقطة بيع جديدة للعميل
  static Future<Map<String, dynamic>> createPos({
    required String name,
    String? address,
    String? category,
  }) async {
    try {
      final url = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.createPos}');
      final headers = await _getHeaders();
      final response = await http.post(
        url,
        headers: headers,
        body: jsonEncode({
          'name': name,
          'address': address,
          'category': category,
        }),
      );

      final data = jsonDecode(response.body);
      return {
        'success': response.statusCode == 200 && data['success'] == true,
        'message': data['message'] ?? '',
        'pos': data['data'] != null ? PosModel.fromJson(data['data']) : null,
      };
    } catch (e) {
      return {'success': false, 'message': 'فشل إنشاء نقطة البيع: $e'};
    }
  }

  /// استرجاع نقاط البيع المملوكة للعميل
  static Future<List<PosModel>> getMyPosPoints() async {
    try {
      final url = Uri.parse(
        '${ApiConstants.baseUrl}${ApiConstants.myPosPoints}',
      );
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] is List) {
          return (data['data'] as List)
              .map((p) => PosModel.fromJson(p))
              .toList();
        }
      }
      return [];
    } catch (_) {
      return [];
    }
  }

  // ==========================================
  // 5. سجل الحركات والعمليات (Transactions)
  // ==========================================

  /// استرجاع العمليات الخاصة بالعميل
  static Future<List<TransactionModel>> getTransactions({
    String? currencyCode,
    String? type,
  }) async {
    try {
      var urlStr = '${ApiConstants.baseUrl}${ApiConstants.getTransactions}?';
      if (currencyCode != null) urlStr += 'currencyCode=$currencyCode&';
      if (type != null) urlStr += 'type=$type&';

      final url = Uri.parse(urlStr.replaceAll(RegExp(r'[?&]$'), ''));
      final headers = await _getHeaders();
      final response = await http.get(url, headers: headers);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        if (data['success'] == true && data['data'] is List) {
          return (data['data'] as List)
              .map((t) => TransactionModel.fromJson(t))
              .toList();
        }
      }
      return [];
    } catch (_) {
      return [];
    }
  }
}
