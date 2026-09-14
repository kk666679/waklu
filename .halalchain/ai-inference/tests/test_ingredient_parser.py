from src.ingredient_parser import assess_risk, parse_ingredients, parse_and_summarize


def test_assess_risk_haram_keyword():
    assert assess_risk("contains pork and lard") == "haram"
    assert assess_risk("ALCOHOL present") == "haram"


def test_assess_risk_mashbooh_keyword():
    assert assess_risk("emulsifier E471") == "mashbooh"
    assert assess_risk("glycerin") == "mashbooh"


def test_assess_risk_halal_default():
    assert assess_risk("sugar, water, salt") == "halal"


def test_parse_ingredients_extracts_e_code_and_percent():
    parsed = parse_ingredients("Sugar 60%, Water, E120")
    by_name = {p["name"].lower().replace(" ", ""): p for p in parsed}
    assert "sugar" in by_name
    assert by_name["sugar"]["percentage"] == "60%"
    assert by_name["sugar"]["risk"] == "halal"
    e120 = next(p for p in parsed if p["eCode"] == "E120")
    assert e120["risk"] == "haram"


def test_parse_and_summarize_counts():
    result = parse_and_summarize("pork, sugar, E471, E120")
    s = result["summary"]
    assert s["total"] == 4
    assert s["haram"] >= 1
    assert "mashbooh" in [i["risk"] for i in result["parsed"]]
