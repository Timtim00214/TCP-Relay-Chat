from google.protobuf.json_format import MessageToDict

def proto_to_dict(proto_obj):
    """
    最强转换器：将任何 Protobuf 对象转为 Python 字典。
    保留默认值，保留枚举名称。
    """
    return MessageToDict(
        proto_obj,
        including_default_value_fields=True, # 即使是0或空也显示，方便调试
        preserving_proto_field_name=True,    # 使用下划线命名
        use_integers_for_enums=False         # 显示枚举的名字而非数字(1)
    )